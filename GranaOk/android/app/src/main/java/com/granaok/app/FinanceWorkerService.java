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

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.util.Properties;
import java.util.concurrent.atomic.AtomicBoolean;

public class FinanceWorkerService extends Service {
    public static final String EXTRA_ACTION = "finance_action";
    public static final String EXTRA_CONFIG = "finance_config";
    public static final String EXTRA_PAYLOAD = "finance_payload";
    public static final String EXTRA_RECEIVER = "finance_receiver";

    public static final String ACTION_LIST_ACCOUNTS = "list_accounts";
    public static final String ACTION_ADD_ACCOUNT = "add_account";
    public static final String ACTION_LIST_CARDS = "list_cards";
    public static final String ACTION_ADD_CARD = "add_card";
    public static final String ACTION_LIST_FINANCINGS = "list_financings";
    public static final String ACTION_ADD_FINANCING = "add_financing";
    public static final String ACTION_OVERVIEW = "overview";

    private static final int CODE_RESULT = 2;
    private static final long HARD_TIMEOUT_MS = 18000L;
    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null) {
            stopSelf(startId);
            return START_NOT_STICKY;
        }

        final String action = intent.getStringExtra(EXTRA_ACTION);
        final String configJson = intent.getStringExtra(EXTRA_CONFIG);
        final String payloadJson = intent.getStringExtra(EXTRA_PAYLOAD);
        final ResultReceiver receiver = intent.getParcelableExtra(EXTRA_RECEIVER);
        final AtomicBoolean delivered = new AtomicBoolean(false);

        final Runnable timeout = () -> {
            if (delivered.compareAndSet(false, true)) {
                send(receiver, fail("A operação excedeu 18 segundos e foi encerrada."));
            }
            stopSelf(startId);
            mainHandler.postDelayed(() -> Process.killProcess(Process.myPid()), 250L);
        };
        mainHandler.postDelayed(timeout, HARD_TIMEOUT_MS);

        Thread worker = new Thread(() -> {
            JSONObject out;
            try {
                JSONObject config = new JSONObject(configJson == null ? "{}" : configJson);
                JSONObject payload = new JSONObject(payloadJson == null ? "{}" : payloadJson);
                switch (action == null ? "" : action) {
                    case ACTION_LIST_ACCOUNTS:
                        out = listAccounts(config);
                        break;
                    case ACTION_ADD_ACCOUNT:
                        out = addAccount(config, payload);
                        break;
                    case ACTION_LIST_CARDS:
                        out = listCards(config);
                        break;
                    case ACTION_ADD_CARD:
                        out = addCard(config, payload);
                        break;
                    case ACTION_LIST_FINANCINGS:
                        out = listFinancings(config);
                        break;
                    case ACTION_ADD_FINANCING:
                        out = addFinancing(config, payload);
                        break;
                    case ACTION_OVERVIEW:
                        out = overview(config);
                        break;
                    default:
                        out = fail("Operação financeira desconhecida.");
                }
            } catch (Throwable e) {
                out = fail(cleanThrowable(e));
            }

            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, out);
                stopSelf(startId);
            }
        }, "GranaOk-Finance-Worker");
        worker.setDaemon(true);
        worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject listAccounts(JSONObject c) throws Exception {
        String p = prefix(c);
        JSONArray rows = new JSONArray();
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            String sql = "SELECT id,name,type,initial_balance,current_balance,active FROM " + p + "accounts ORDER BY active DESC,name ASC";
            try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("id", rs.getLong(1));
                    x.put("name", rs.getString(2));
                    x.put("type", rs.getString(3));
                    x.put("initial_balance", rs.getDouble(4));
                    x.put("current_balance", rs.getDouble(5));
                    x.put("active", rs.getInt(6) == 1);
                    rows.put(x);
                }
            }
        }
        return okRows(rows);
    }

    private JSONObject addAccount(JSONObject c, JSONObject a) throws Exception {
        String name = a.optString("name", "").trim();
        String type = a.optString("type", "bank").trim();
        BigDecimal initial = decimal(a.optString("initialBalance", "0"));
        BigDecimal current = decimal(a.optString("currentBalance", initial.toPlainString()));
        if (name.isEmpty()) throw new IllegalArgumentException("Informe o nome da conta.");
        if (!type.matches("[A-Za-z0-9_-]{1,30}")) type = "bank";

        String p = prefix(c);
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            try (PreparedStatement ps = conn.prepareStatement(
                "INSERT INTO " + p + "accounts(name,type,initial_balance,current_balance,active) VALUES(?,?,?,?,1)")) {
                ps.setString(1, name);
                ps.setString(2, type);
                ps.setBigDecimal(3, initial);
                ps.setBigDecimal(4, current);
                ps.executeUpdate();
            }
        }
        return ok("Conta cadastrada.");
    }

    private JSONObject listCards(JSONObject c) throws Exception {
        String p = prefix(c);
        JSONArray rows = new JSONArray();
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            String sql = "SELECT c.id,c.name,c.closing_day,c.due_day,c.limit_amount,c.active," +
                "COALESCE((SELECT SUM(i.amount) FROM " + p + "card_invoices i WHERE i.card_id=c.id AND DATE_FORMAT(i.due_date,'%Y-%m')=DATE_FORMAT(CURDATE(),'%Y-%m')),0) invoice_amount " +
                "FROM " + p + "cards c ORDER BY c.active DESC,c.name ASC";
            try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("id", rs.getLong(1));
                    x.put("name", rs.getString(2));
                    x.put("closing_day", rs.getInt(3));
                    x.put("due_day", rs.getInt(4));
                    x.put("limit_amount", rs.getDouble(5));
                    x.put("active", rs.getInt(6) == 1);
                    x.put("invoice_amount", rs.getDouble(7));
                    rows.put(x);
                }
            }
        }
        return okRows(rows);
    }

    private JSONObject addCard(JSONObject c, JSONObject card) throws Exception {
        String name = card.optString("name", "").trim();
        int closing = card.optInt("closingDay", 1);
        int due = card.optInt("dueDay", 10);
        BigDecimal limit = decimal(card.optString("limitAmount", "0"));
        if (name.isEmpty()) throw new IllegalArgumentException("Informe o nome do cartão.");
        if (closing < 1 || closing > 31) throw new IllegalArgumentException("Dia de fechamento deve ficar entre 1 e 31.");
        if (due < 1 || due > 31) throw new IllegalArgumentException("Dia de vencimento deve ficar entre 1 e 31.");

        String p = prefix(c);
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            try (PreparedStatement ps = conn.prepareStatement(
                "INSERT INTO " + p + "cards(name,closing_day,due_day,limit_amount,active) VALUES(?,?,?,?,1)")) {
                ps.setString(1, name);
                ps.setInt(2, closing);
                ps.setInt(3, due);
                ps.setBigDecimal(4, limit);
                ps.executeUpdate();
            }
        }
        return ok("Cartão cadastrado.");
    }

    private JSONObject listFinancings(JSONObject c) throws Exception {
        String p = prefix(c);
        JSONArray rows = new JSONArray();
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            String sql = "SELECT id,name,total_amount,installment_amount,total_installments,paid_installments,active FROM " + p + "financings ORDER BY active DESC,id DESC";
            try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
                while (rs.next()) {
                    int total = rs.getInt(5);
                    int paid = rs.getInt(6);
                    JSONObject x = new JSONObject();
                    x.put("id", rs.getLong(1));
                    x.put("name", rs.getString(2));
                    x.put("total_amount", rs.getDouble(3));
                    x.put("installment_amount", rs.getDouble(4));
                    x.put("total_installments", total);
                    x.put("paid_installments", paid);
                    x.put("remaining_installments", Math.max(0, total - paid));
                    x.put("active", rs.getInt(7) == 1);
                    rows.put(x);
                }
            }
        }
        return okRows(rows);
    }

    private JSONObject addFinancing(JSONObject c, JSONObject f) throws Exception {
        String name = f.optString("name", "").trim();
        BigDecimal totalAmount = decimal(f.optString("totalAmount", "0"));
        BigDecimal installmentAmount = decimal(f.optString("installmentAmount", "0"));
        int totalInstallments = f.optInt("totalInstallments", 0);
        int paidInstallments = f.optInt("paidInstallments", 0);
        if (name.isEmpty()) throw new IllegalArgumentException("Informe o nome do financiamento.");
        if (totalInstallments < 1) throw new IllegalArgumentException("Informe a quantidade total de parcelas.");
        if (paidInstallments < 0 || paidInstallments > totalInstallments) {
            throw new IllegalArgumentException("Parcelas pagas não podem ser maiores que o total.");
        }
        if (installmentAmount.compareTo(BigDecimal.ZERO) <= 0) {
            throw new IllegalArgumentException("Informe o valor da parcela.");
        }

        String p = prefix(c);
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            try (PreparedStatement ps = conn.prepareStatement(
                "INSERT INTO " + p + "financings(name,total_amount,installment_amount,total_installments,paid_installments,active) VALUES(?,?,?,?,?,1)")) {
                ps.setString(1, name);
                ps.setBigDecimal(2, totalAmount);
                ps.setBigDecimal(3, installmentAmount);
                ps.setInt(4, totalInstallments);
                ps.setInt(5, paidInstallments);
                ps.executeUpdate();
            }
        }
        return ok("Financiamento cadastrado.");
    }

    private JSONObject overview(JSONObject c) throws Exception {
        String p = prefix(c);
        JSONObject out = new JSONObject();
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            out.put("ok", true);
            out.put("accounts_count", scalarInt(conn, "SELECT COUNT(*) FROM " + p + "accounts WHERE active=1"));
            out.put("accounts_balance", scalarDouble(conn, "SELECT COALESCE(SUM(current_balance),0) FROM " + p + "accounts WHERE active=1"));
            out.put("cards_count", scalarInt(conn, "SELECT COUNT(*) FROM " + p + "cards WHERE active=1"));
            out.put("cards_limit", scalarDouble(conn, "SELECT COALESCE(SUM(limit_amount),0) FROM " + p + "cards WHERE active=1"));
            out.put("financings_count", scalarInt(conn, "SELECT COUNT(*) FROM " + p + "financings WHERE active=1"));
            out.put("financing_monthly", scalarDouble(conn, "SELECT COALESCE(SUM(installment_amount),0) FROM " + p + "financings WHERE active=1"));
        }
        return out;
    }

    private void ensureTables(Connection conn, String p) throws Exception {
        String[] sql = new String[]{
            "CREATE TABLE IF NOT EXISTS " + p + "accounts (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,type VARCHAR(40) NOT NULL DEFAULT 'bank',initial_balance DECIMAL(14,2) NOT NULL DEFAULT 0,current_balance DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_person(person_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "cards (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,closing_day INT NOT NULL DEFAULT 1,due_day INT NOT NULL DEFAULT 10,limit_amount DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "card_invoices (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,card_id BIGINT UNSIGNED NOT NULL,reference_month DATE NOT NULL,due_date DATE NOT NULL,amount DECIMAL(14,2) NOT NULL DEFAULT 0,status VARCHAR(20) NOT NULL DEFAULT 'open',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_invoice_due(due_date)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "financings (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(160) NOT NULL,total_amount DECIMAL(14,2) NOT NULL DEFAULT 0,installment_amount DECIMAL(14,2) NOT NULL DEFAULT 0,total_installments INT NOT NULL DEFAULT 0,paid_installments INT NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"
        };
        try (Statement st = conn.createStatement()) {
            for (String q : sql) st.execute(q);
        }
    }

    private Connection openConnection(JSONObject c) throws Exception {
        String host = c.optString("host", "").trim();
        int port = c.optInt("port", 3306);
        String database = c.optString("database", "").trim();
        String user = c.optString("user", "").trim();
        String password = c.optString("password", "");
        boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) {
            throw new IllegalArgumentException("Configuração MySQL incompleta.");
        }

        StringBuilder url = new StringBuilder("jdbc:mariadb://")
            .append(host).append(':').append(port).append('/').append(database)
            .append("?connectTimeout=5000&socketTimeout=7000&tcpKeepAlive=true");
        if (ssl) url.append("&useSsl=true&trustServerCertificate=true");
        else url.append("&useSsl=false");

        Properties props = new Properties();
        props.setProperty("user", user);
        props.setProperty("password", password);
        UrlParser parser = UrlParser.parse(url.toString(), props);
        if (parser == null || parser.getHostAddresses() == null) {
            throw new IllegalArgumentException("Connection string MySQL inválida.");
        }
        return MariaDbConnection.newConnection(parser, null);
    }

    private static BigDecimal decimal(String value) {
        String v = value == null ? "0" : value.trim().replace(".", "").replace(',', '.');
        if (v.isEmpty()) v = "0";
        return new BigDecimal(v);
    }

    private static String prefix(JSONObject c) {
        String p = c.optString("prefix", "granaok_");
        return p.matches("[A-Za-z0-9_]{1,32}") ? p : "granaok_";
    }

    private static int scalarInt(Connection conn, String sql) throws Exception {
        try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
            return rs.next() ? rs.getInt(1) : 0;
        }
    }

    private static double scalarDouble(Connection conn, String sql) throws Exception {
        try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
            return rs.next() ? rs.getDouble(1) : 0d;
        }
    }

    private static JSONObject okRows(JSONArray rows) throws Exception {
        JSONObject out = new JSONObject();
        out.put("ok", true);
        out.put("rows", rows);
        return out;
    }

    private static JSONObject ok(String message) throws Exception {
        JSONObject out = new JSONObject();
        out.put("ok", true);
        out.put("message", message);
        return out;
    }

    private static JSONObject fail(String message) {
        JSONObject out = new JSONObject();
        try {
            out.put("ok", false);
            out.put("error", message);
        } catch (Throwable ignored) {
        }
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
        } catch (Throwable ignored) {
        }
    }
}
