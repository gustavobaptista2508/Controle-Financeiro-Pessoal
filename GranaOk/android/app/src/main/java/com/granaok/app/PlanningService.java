package com.granaok.app;

import android.app.Service;
import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Process;
import android.os.ResultReceiver;

import org.json.JSONArray;
import org.json.JSONObject;
import org.mariadb.jdbc.MariaDbConnection;
import org.mariadb.jdbc.UrlParser;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Locale;
import java.util.Properties;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * Planejamento mensal do GranaOk.
 * Beta 0.5.3: o saldo projetado passa a partir do saldo REAL atual das contas
 * e considera apenas movimentos ainda pendentes, evitando contabilizar novamente
 * valores pagos que ja fazem parte do current_balance.
 */
public class PlanningService extends Service {
    public static final String EXTRA_ACTION = "planning_action";
    public static final String EXTRA_CONFIG = "planning_config";
    public static final String EXTRA_PAYLOAD = "planning_payload";
    public static final String EXTRA_RECEIVER = "planning_receiver";

    public static final String ACTION_MONTH_SUMMARY = "month_summary";
    public static final String ACTION_NEXT_PROJECTION = "next_projection";

    private static final int CODE_RESULT = 2;
    private static final long HARD_TIMEOUT_MS = 18000L;
    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    @Override public IBinder onBind(Intent intent) { return null; }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null) { stopSelf(startId); return START_NOT_STICKY; }
        final String action = intent.getStringExtra(EXTRA_ACTION);
        final String configJson = intent.getStringExtra(EXTRA_CONFIG);
        final String payloadJson = intent.getStringExtra(EXTRA_PAYLOAD);
        final ResultReceiver receiver = intent.getParcelableExtra(EXTRA_RECEIVER);
        final AtomicBoolean delivered = new AtomicBoolean(false);

        final Runnable timeout = () -> {
            if (delivered.compareAndSet(false, true)) send(receiver, fail("A consulta de planejamento excedeu 18 segundos e foi encerrada."));
            stopSelf(startId);
            mainHandler.postDelayed(() -> Process.killProcess(Process.myPid()), 250L);
        };
        mainHandler.postDelayed(timeout, HARD_TIMEOUT_MS);

        Thread worker = new Thread(() -> {
            JSONObject out;
            try {
                JSONObject config = new JSONObject(configJson == null ? "{}" : configJson);
                JSONObject payload = new JSONObject(payloadJson == null ? "{}" : payloadJson);
                if (ACTION_MONTH_SUMMARY.equals(action)) out = monthSummary(config, payload);
                else if (ACTION_NEXT_PROJECTION.equals(action)) out = nextProjection(config, payload);
                else out = fail("Operação de planejamento desconhecida.");
            } catch (Throwable e) {
                out = fail(cleanThrowable(e));
            }
            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, out);
                stopSelf(startId);
            }
        }, "GranaOk-Planning-Worker");
        worker.setDaemon(true);
        worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject monthSummary(JSONObject config, JSONObject payload) throws Exception {
        String month = normalizeMonth(payload.optString("month", currentMonth()));
        String p = prefix(config);
        try (Connection conn = openConnection(config)) {
            return summaryForMonth(conn, p, month);
        }
    }

    private JSONObject nextProjection(JSONObject config, JSONObject payload) throws Exception {
        String baseMonth = normalizeMonth(payload.optString("month", currentMonth()));
        String nextMonth = shiftMonth(baseMonth, 1);
        String p = prefix(config);
        try (Connection conn = openConnection(config)) {
            JSONObject base = summaryForMonth(conn, p, baseMonth);
            JSONObject next = summaryForMonth(conn, p, nextMonth);

            double registeredIncome = next.optDouble("pending_income_month", 0d);
            double baselineIncome = base.optDouble("income", 0d);
            double scenarioIncome = registeredIncome > 0d ? registeredIncome : baselineIncome;
            double nextExpenses = next.optDouble("pending_expenses_month", 0d);
            double nextInvoices = next.optDouble("pending_card_invoices_month", 0d);
            double nextFinancing = next.optDouble("pending_financing_month", 0d);
            double registeredOutflows = nextExpenses + nextInvoices + nextFinancing;
            double baseProjected = base.optDouble("projected_balance", base.optDouble("accounts_balance", 0d));
            double scenarioBalance = baseProjected + scenarioIncome - registeredOutflows;

            next.put("ok", true);
            next.put("projection", true);
            next.put("base_month", baseMonth);
            next.put("base_income", baselineIncome);
            next.put("base_expenses", base.optDouble("expenses", 0d));
            next.put("base_card_invoices", base.optDouble("card_invoices", 0d));
            next.put("base_projected_balance", baseProjected);
            next.put("scenario_income", scenarioIncome);
            next.put("scenario_balance", scenarioBalance);
            next.put("income_inferred", registeredIncome <= 0d && baselineIncome > 0d);
            next.put("registered_outflows", registeredOutflows);
            return next;
        }
    }

    private JSONObject summaryForMonth(Connection conn, String p, String month) throws Exception {
        JSONObject out = new JSONObject();
        out.put("ok", true);
        out.put("month", month);

        // Totais de competencia continuam sendo exibidos para analise mensal.
        double income = scalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='income' AND DATE_FORMAT(due_date,'%Y-%m')=?",
            month);
        double expenses = scalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='expense' AND COALESCE(source,'manual')<>'card_invoice' AND DATE_FORMAT(due_date,'%Y-%m')=?",
            month);
        double invoices = scalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "card_invoices WHERE DATE_FORMAT(due_date,'%Y-%m')=?",
            month);

        String previous = shiftMonth(month, -1);
        double prevIncome = scalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='income' AND DATE_FORMAT(due_date,'%Y-%m')=?",
            previous);
        double prevExpenses = scalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='expense' AND COALESCE(source,'manual')<>'card_invoice' AND DATE_FORMAT(due_date,'%Y-%m')=?",
            previous);

        double accountsBalance = safeScalar(conn,
            "SELECT COALESCE(SUM(current_balance),0) FROM " + p + "accounts WHERE active=1");

        // Pendencias exclusivas do mes selecionado (usadas pela IA e pelos cards de detalhe).
        double pendingIncomeMonth = scalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='income' AND COALESCE(status,'pending')<>'paid' AND DATE_FORMAT(due_date,'%Y-%m')=?",
            month);
        double pendingExpensesMonth = scalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='expense' AND COALESCE(status,'pending')<>'paid' AND COALESCE(source,'manual')<>'card_invoice' AND DATE_FORMAT(due_date,'%Y-%m')=?",
            month);
        double pendingInvoicesMonth = scalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "card_invoices WHERE COALESCE(status,'open')<>'paid' AND DATE_FORMAT(due_date,'%Y-%m')=?",
            month);
        double pendingFinancingMonth = safeScalarMonth(conn,
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "financing_installments WHERE COALESCE(status,'pending')<>'paid' AND DATE_FORMAT(due_date,'%Y-%m')=?",
            month);

        double projectedBalance;
        String balanceMode;
        if (month.compareTo(currentMonth()) >= 0) {
            String endDate = monthEnd(month);
            double pendingIncomeThroughTarget = scalarDate(conn,
                "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='income' AND COALESCE(status,'pending')<>'paid' AND due_date<=?",
                endDate);
            double pendingExpensesThroughTarget = scalarDate(conn,
                "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='expense' AND COALESCE(status,'pending')<>'paid' AND COALESCE(source,'manual')<>'card_invoice' AND due_date<=?",
                endDate);
            double pendingInvoicesThroughTarget = scalarDate(conn,
                "SELECT COALESCE(SUM(amount),0) FROM " + p + "card_invoices WHERE COALESCE(status,'open')<>'paid' AND due_date<=?",
                endDate);
            double pendingFinancingThroughTarget = safeScalarDate(conn,
                "SELECT COALESCE(SUM(amount),0) FROM " + p + "financing_installments WHERE COALESCE(status,'pending')<>'paid' AND due_date<=?",
                endDate);

            projectedBalance = accountsBalance
                + pendingIncomeThroughTarget
                - pendingExpensesThroughTarget
                - pendingInvoicesThroughTarget
                - pendingFinancingThroughTarget;
            balanceMode = "real_balance_plus_pending";

            out.put("pending_income_through_target", pendingIncomeThroughTarget);
            out.put("pending_expenses_through_target", pendingExpensesThroughTarget);
            out.put("pending_card_invoices_through_target", pendingInvoicesThroughTarget);
            out.put("pending_financing_through_target", pendingFinancingThroughTarget);
        } else {
            // Mes passado: sem snapshot historico confiavel de current_balance, mostra fluxo liquido da competencia.
            projectedBalance = income - expenses - invoices;
            balanceMode = "historical_month_cashflow";
        }

        out.put("income", income);
        out.put("expenses", expenses);
        out.put("card_invoices", invoices);
        out.put("projected_balance", projectedBalance);
        out.put("balance_mode", balanceMode);
        out.put("prev_income", prevIncome);
        out.put("prev_expenses", prevExpenses);
        out.put("accounts_balance", accountsBalance);
        out.put("pending_income_month", pendingIncomeMonth);
        out.put("pending_expenses_month", pendingExpensesMonth);
        out.put("pending_card_invoices_month", pendingInvoicesMonth);
        out.put("pending_financing_month", pendingFinancingMonth);
        out.put("financing_monthly", safeScalar(conn,
            "SELECT COALESCE(SUM(installment_amount),0) FROM " + p + "financings WHERE active=1"));

        JSONArray categories = new JSONArray();
        String qCategories = "SELECT COALESCE(c.name,'Sem categoria'),COALESCE(SUM(t.amount),0) total " +
            "FROM " + p + "transactions t LEFT JOIN " + p + "categories c ON c.id=t.category_id " +
            "WHERE t.type='expense' AND COALESCE(t.source,'manual')<>'card_invoice' AND DATE_FORMAT(t.due_date,'%Y-%m')=? " +
            "GROUP BY c.name ORDER BY total DESC LIMIT 10";
        try (PreparedStatement ps = conn.prepareStatement(qCategories)) {
            ps.setString(1, month);
            try (ResultSet rs = ps.executeQuery()) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("name", rs.getString(1));
                    x.put("total", rs.getDouble(2));
                    categories.put(x);
                }
            }
        }
        out.put("categories", categories);

        JSONArray scheduled = new JSONArray();
        String qScheduled = "SELECT description,due_date,amount,type,status FROM " + p + "transactions " +
            "WHERE DATE_FORMAT(due_date,'%Y-%m')=? ORDER BY due_date ASC,id ASC LIMIT 20";
        try (PreparedStatement ps = conn.prepareStatement(qScheduled)) {
            ps.setString(1, month);
            try (ResultSet rs = ps.executeQuery()) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("description", rs.getString(1));
                    x.put("due_date", rs.getString(2));
                    x.put("amount", rs.getDouble(3));
                    x.put("type", rs.getString(4));
                    x.put("status", rs.getString(5));
                    scheduled.put(x);
                }
            }
        }
        out.put("scheduled", scheduled);
        out.put("upcoming", scheduled);
        return out;
    }

    private double scalarMonth(Connection conn, String sql, String month) throws Exception {
        try (PreparedStatement ps = conn.prepareStatement(sql)) {
            ps.setString(1, month);
            try (ResultSet rs = ps.executeQuery()) { return rs.next() ? rs.getDouble(1) : 0d; }
        }
    }

    private double safeScalarMonth(Connection conn, String sql, String month) {
        try { return scalarMonth(conn, sql, month); } catch (Throwable ignored) { return 0d; }
    }

    private double scalarDate(Connection conn, String sql, String date) throws Exception {
        try (PreparedStatement ps = conn.prepareStatement(sql)) {
            ps.setString(1, date);
            try (ResultSet rs = ps.executeQuery()) { return rs.next() ? rs.getDouble(1) : 0d; }
        }
    }

    private double safeScalarDate(Connection conn, String sql, String date) {
        try { return scalarDate(conn, sql, date); } catch (Throwable ignored) { return 0d; }
    }

    private double safeScalar(Connection conn, String sql) {
        try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
            return rs.next() ? rs.getDouble(1) : 0d;
        } catch (Throwable ignored) { return 0d; }
    }

    private Connection openConnection(JSONObject c) throws Exception {
        String host = c.optString("host", "").trim();
        int port = c.optInt("port", 3306);
        String database = c.optString("database", "").trim();
        String user = c.optString("user", "").trim();
        String password = c.optString("password", "");
        boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) throw new IllegalArgumentException("Configuração MySQL incompleta.");
        StringBuilder url = new StringBuilder("jdbc:mariadb://")
            .append(host).append(':').append(port).append('/').append(database)
            .append("?connectTimeout=5000&socketTimeout=7000&tcpKeepAlive=true");
        if (ssl) url.append("&useSsl=true&trustServerCertificate=true"); else url.append("&useSsl=false");
        Properties props = new Properties();
        props.setProperty("user", user); props.setProperty("password", password);
        UrlParser parser = UrlParser.parse(url.toString(), props);
        if (parser == null || parser.getHostAddresses() == null) throw new IllegalArgumentException("Connection string MySQL inválida.");
        return MariaDbConnection.newConnection(parser, null);
    }

    private static String currentMonth() {
        return new SimpleDateFormat("yyyy-MM", Locale.US).format(Calendar.getInstance().getTime());
    }

    private static String normalizeMonth(String month) {
        String value = month == null ? "" : month.trim();
        if (!value.matches("\\d{4}-(0[1-9]|1[0-2])")) return currentMonth();
        return value;
    }

    private static String shiftMonth(String month, int delta) throws Exception {
        SimpleDateFormat parser = new SimpleDateFormat("yyyy-MM", Locale.US);
        parser.setLenient(false);
        Calendar cal = Calendar.getInstance();
        cal.setTime(parser.parse(normalizeMonth(month)));
        cal.add(Calendar.MONTH, delta);
        return parser.format(cal.getTime());
    }

    private static String monthEnd(String month) throws Exception {
        SimpleDateFormat parser = new SimpleDateFormat("yyyy-MM-dd", Locale.US);
        parser.setLenient(false);
        Calendar cal = Calendar.getInstance();
        cal.setTime(parser.parse(normalizeMonth(month) + "-01"));
        cal.set(Calendar.DAY_OF_MONTH, cal.getActualMaximum(Calendar.DAY_OF_MONTH));
        return parser.format(cal.getTime());
    }

    private static String prefix(JSONObject c) {
        String p = c.optString("prefix", "granaok_");
        return p.matches("[A-Za-z0-9_]{1,32}") ? p : "granaok_";
    }

    private static JSONObject fail(String message) {
        JSONObject out = new JSONObject();
        try { out.put("ok", false); out.put("error", message); } catch (Throwable ignored) {}
        return out;
    }

    private static String cleanThrowable(Throwable e) {
        String m = e == null ? "Falha desconhecida" : e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        m = m.replaceAll("(?i)password=[^&\\s]+", "password=***");
        m = m.replaceAll("(?i)(password|senha)[=:]\\s*[^\\s,;]+", "$1=***");
        return m.length() > 500 ? m.substring(0, 500) : m;
    }

    private static void send(ResultReceiver receiver, JSONObject payload) {
        if (receiver == null) return;
        try {
            Bundle bundle = new Bundle();
            bundle.putString("json", payload == null ? "{}" : payload.toString());
            receiver.send(CODE_RESULT, bundle);
        } catch (Throwable ignored) {}
    }
}
