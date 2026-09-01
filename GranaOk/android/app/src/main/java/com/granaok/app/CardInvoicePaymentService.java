package com.granaok.app;

import android.app.Service;
import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Process;
import android.os.ResultReceiver;

import org.json.JSONObject;
import org.mariadb.jdbc.MariaDbConnection;
import org.mariadb.jdbc.UrlParser;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Locale;
import java.util.Properties;
import java.util.concurrent.atomic.AtomicBoolean;

/** Beta 0.6.0: baixa/reabertura de faturas de cartão. */
public class CardInvoicePaymentService extends Service {
    public static final String EXTRA_ACTION = "invoice_payment_action";
    public static final String EXTRA_CONFIG = "invoice_payment_config";
    public static final String EXTRA_PAYLOAD = "invoice_payment_payload";
    public static final String EXTRA_RECEIVER = "invoice_payment_receiver";

    public static final String ACTION_PAY = "card_invoice_pay_v060";
    public static final String ACTION_REOPEN = "card_invoice_reopen_v060";

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
            if (delivered.compareAndSet(false, true)) send(receiver, fail("A operação excedeu 18 segundos e foi encerrada."));
            stopSelf(startId);
            mainHandler.postDelayed(() -> Process.killProcess(Process.myPid()), 250L);
        };
        mainHandler.postDelayed(timeout, HARD_TIMEOUT_MS);

        Thread worker = new Thread(() -> {
            JSONObject out;
            try {
                JSONObject c = new JSONObject(configJson == null ? "{}" : configJson);
                JSONObject p = new JSONObject(payloadJson == null ? "{}" : payloadJson);
                if (ACTION_PAY.equals(action)) out = pay(c, p);
                else if (ACTION_REOPEN.equals(action)) out = reopen(c, p);
                else out = fail("Operação de fatura desconhecida.");
            } catch (Throwable e) {
                out = fail(clean(e));
            }
            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, out);
                stopSelf(startId);
            }
        }, "GranaOk-Invoice-Payment");
        worker.setDaemon(true);
        worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject pay(JSONObject c, JSONObject pld) throws Exception {
        long cardId = pld.optLong("cardId", 0);
        String month = normalizeMonth(pld.optString("month", ""));
        String paidDate = pld.optString("paidDate", today()).trim();
        if (cardId <= 0) throw new IllegalArgumentException("Cartão inválido.");
        validateDate(paidDate);

        String p = prefix(c);
        try (Connection conn = open(c)) {
            ensureSchema(conn, p);
            conn.setAutoCommit(false);
            try {
                InvoiceData data = invoiceData(conn, p, cardId, month);
                Long invoiceId = findInvoice(conn, p, cardId, month);
                if (invoiceId == null) {
                    try (PreparedStatement ps = conn.prepareStatement(
                        "INSERT INTO " + p + "card_invoices(card_id,reference_month,due_date,amount,status,paid_date) VALUES(?,?,?,?, 'paid',?)")) {
                        ps.setLong(1, cardId);
                        ps.setString(2, month + "-01");
                        ps.setString(3, data.dueDate);
                        ps.setBigDecimal(4, data.total);
                        ps.setString(5, paidDate);
                        ps.executeUpdate();
                    }
                } else {
                    try (PreparedStatement ps = conn.prepareStatement(
                        "UPDATE " + p + "card_invoices SET amount=?,due_date=?,status='paid',paid_date=? WHERE id=?")) {
                        ps.setBigDecimal(1, data.total);
                        ps.setString(2, data.dueDate);
                        ps.setString(3, paidDate);
                        ps.setLong(4, invoiceId);
                        ps.executeUpdate();
                    }
                }
                conn.commit();
            } catch (Throwable e) {
                try { conn.rollback(); } catch (Throwable ignored) {}
                throw e;
            } finally {
                try { conn.setAutoCommit(true); } catch (Throwable ignored) {}
            }
        }
        JSONObject out = ok();
        out.put("message", "Fatura marcada como paga.");
        out.put("status", "paid");
        out.put("card_id", cardId);
        out.put("month", month);
        out.put("paid_date", paidDate);
        return out;
    }

    private JSONObject reopen(JSONObject c, JSONObject pld) throws Exception {
        long cardId = pld.optLong("cardId", 0);
        String month = normalizeMonth(pld.optString("month", ""));
        if (cardId <= 0) throw new IllegalArgumentException("Cartão inválido.");
        String p = prefix(c);
        try (Connection conn = open(c)) {
            ensureSchema(conn, p);
            try (PreparedStatement ps = conn.prepareStatement(
                "UPDATE " + p + "card_invoices SET status='open',paid_date=NULL WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=?")) {
                ps.setLong(1, cardId);
                ps.setString(2, month);
                if (ps.executeUpdate() == 0) throw new IllegalArgumentException("Fatura não encontrada.");
            }
        }
        JSONObject out = ok();
        out.put("message", "Fatura reaberta.");
        out.put("status", "open");
        out.put("card_id", cardId);
        out.put("month", month);
        return out;
    }

    private InvoiceData invoiceData(Connection conn, String p, long cardId, String month) throws Exception {
        int dueDay;
        try (PreparedStatement ps = conn.prepareStatement("SELECT due_day FROM " + p + "cards WHERE id=? LIMIT 1")) {
            ps.setLong(1, cardId);
            try (ResultSet rs = ps.executeQuery()) {
                if (!rs.next()) throw new IllegalArgumentException("Cartão não encontrado.");
                dueDay = rs.getInt(1);
            }
        }
        BigDecimal total = BigDecimal.ZERO;
        try (PreparedStatement ps = conn.prepareStatement(
            "SELECT COALESCE(SUM(amount),0) FROM " + p + "card_purchases WHERE card_id=? AND DATE_FORMAT(due_date,'%Y-%m')=?")) {
            ps.setLong(1, cardId);
            ps.setString(2, month);
            try (ResultSet rs = ps.executeQuery()) { if (rs.next()) total = rs.getBigDecimal(1); }
        }
        Calendar cal = Calendar.getInstance();
        cal.setTime(new SimpleDateFormat("yyyy-MM-dd", Locale.US).parse(month + "-01"));
        cal.set(Calendar.DAY_OF_MONTH, Math.min(Math.max(1, dueDay), cal.getActualMaximum(Calendar.DAY_OF_MONTH)));
        return new InvoiceData(total == null ? BigDecimal.ZERO : total, new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(cal.getTime()));
    }

    private Long findInvoice(Connection conn, String p, long cardId, String month) throws Exception {
        try (PreparedStatement ps = conn.prepareStatement(
            "SELECT id FROM " + p + "card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? ORDER BY id LIMIT 1")) {
            ps.setLong(1, cardId);
            ps.setString(2, month);
            try (ResultSet rs = ps.executeQuery()) { return rs.next() ? rs.getLong(1) : null; }
        }
    }

    private void ensureSchema(Connection conn, String p) throws Exception {
        try (Statement st = conn.createStatement()) {
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "card_invoices (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,card_id BIGINT UNSIGNED NOT NULL,reference_month DATE NOT NULL,due_date DATE NOT NULL,amount DECIMAL(14,2) NOT NULL DEFAULT 0,status VARCHAR(20) NOT NULL DEFAULT 'open',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_invoice_due(due_date),KEY idx_card_ref(card_id,reference_month)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
        }
        addColumn(conn, p + "card_invoices", "paid_date", "DATE NULL");
    }

    private static void addColumn(Connection conn, String table, String column, String def) throws Exception {
        try (Statement st = conn.createStatement()) { st.execute("ALTER TABLE " + table + " ADD COLUMN " + column + " " + def); }
        catch (java.sql.SQLException e) { if (!("42S21".equals(e.getSQLState()) || e.getErrorCode() == 1060)) throw e; }
    }

    private Connection open(JSONObject c) throws Exception {
        String host = c.optString("host", "").trim();
        int port = c.optInt("port", 3306);
        String database = c.optString("database", "").trim();
        String user = c.optString("user", "").trim();
        String password = c.optString("password", "");
        boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) throw new IllegalArgumentException("Configuração MySQL incompleta.");
        String url = "jdbc:mariadb://" + host + ":" + port + "/" + database +
            "?connectTimeout=5000&socketTimeout=8000&tcpKeepAlive=true" +
            (ssl ? "&useSsl=true&trustServerCertificate=true" : "&useSsl=false");
        Properties props = new Properties();
        props.setProperty("user", user);
        props.setProperty("password", password);
        UrlParser parser = UrlParser.parse(url, props);
        if (parser == null || parser.getHostAddresses() == null) throw new IllegalArgumentException("Connection string MySQL inválida.");
        return MariaDbConnection.newConnection(parser, null);
    }

    private static String normalizeMonth(String m) {
        return m != null && m.matches("\\d{4}-(0[1-9]|1[0-2])") ? m : new SimpleDateFormat("yyyy-MM", Locale.US).format(new java.util.Date());
    }
    private static void validateDate(String d) throws Exception {
        if (d == null || !d.matches("\\d{4}-\\d{2}-\\d{2}")) throw new IllegalArgumentException("Data de pagamento inválida.");
        SimpleDateFormat f = new SimpleDateFormat("yyyy-MM-dd", Locale.US); f.setLenient(false); f.parse(d);
    }
    private static String today() { return new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new java.util.Date()); }
    private static String prefix(JSONObject c) { String p = c.optString("prefix", "granaok_"); return p.matches("[A-Za-z0-9_]{1,32}") ? p : "granaok_"; }
    private static JSONObject ok() { JSONObject o = new JSONObject(); try { o.put("ok", true); } catch (Throwable ignored) {} return o; }
    private static JSONObject fail(String m) { JSONObject o = new JSONObject(); try { o.put("ok", false); o.put("error", m); } catch (Throwable ignored) {} return o; }
    private static String clean(Throwable e) {
        String m = e == null ? "Falha desconhecida" : e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        return m.replaceAll("(?i)password=[^&\\s]+", "password=***");
    }
    private static void send(ResultReceiver receiver, JSONObject payload) {
        if (receiver == null) return;
        try { Bundle b = new Bundle(); b.putString("json", payload == null ? "{}" : payload.toString()); receiver.send(CODE_RESULT, b); } catch (Throwable ignored) {}
    }
    private static final class InvoiceData {
        final BigDecimal total; final String dueDate;
        InvoiceData(BigDecimal total, String dueDate) { this.total = total; this.dueDate = dueDate; }
    }
}
