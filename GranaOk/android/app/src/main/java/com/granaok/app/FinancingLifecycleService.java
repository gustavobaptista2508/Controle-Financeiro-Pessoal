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
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.Locale;
import java.util.Properties;
import java.util.concurrent.atomic.AtomicBoolean;

/** Controle detalhado de parcelas dos financiamentos. */
public class FinancingLifecycleService extends Service {
    public static final String EXTRA_ACTION = "financing_action";
    public static final String EXTRA_CONFIG = "financing_config";
    public static final String EXTRA_PAYLOAD = "financing_payload";
    public static final String EXTRA_RECEIVER = "financing_receiver";

    public static final String ACTION_LIST = "financing_list_v052";
    public static final String ACTION_ADD = "financing_add_v052";
    public static final String ACTION_SET_NEXT_DUE = "financing_set_next_due_v052";
    public static final String ACTION_PAY_CURRENT = "financing_pay_current_v052";
    public static final String ACTION_INSTALLMENTS = "financing_installments_v052";

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
                JSONObject config = new JSONObject(configJson == null ? "{}" : configJson);
                JSONObject payload = new JSONObject(payloadJson == null ? "{}" : payloadJson);
                if (ACTION_LIST.equals(action)) out = list(config);
                else if (ACTION_ADD.equals(action)) out = add(config, payload);
                else if (ACTION_SET_NEXT_DUE.equals(action)) out = setNextDue(config, payload);
                else if (ACTION_PAY_CURRENT.equals(action)) out = payCurrent(config, payload);
                else if (ACTION_INSTALLMENTS.equals(action)) out = installments(config, payload);
                else out = fail("Operação de financiamento desconhecida.");
            } catch (Throwable e) {
                out = fail(clean(e));
            }
            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, out);
                stopSelf(startId);
            }
        }, "GranaOk-Financing-Worker");
        worker.setDaemon(true);
        worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject list(JSONObject c) throws Exception {
        String p = prefix(c);
        JSONArray rows = new JSONArray();
        try (Connection conn = open(c)) {
            ensureSchema(conn, p);
            String sql = "SELECT id,name,total_amount,installment_amount,total_installments,paid_installments,active,next_due_date,last_paid_date,completed_at FROM " + p + "financings ORDER BY active DESC,id DESC";
            try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
                while (rs.next()) {
                    long id = rs.getLong(1);
                    int total = rs.getInt(5), paid = rs.getInt(6);
                    String nextDue = rs.getString(8);
                    if (paid < total && nextDue != null && !nextDue.trim().isEmpty() && pendingCount(conn, p, id) == 0) {
                        generatePendingSchedule(conn, p, id, paid, total, rs.getBigDecimal(4), nextDue);
                    }
                    JSONObject current = firstPending(conn, p, id);
                    JSONObject x = new JSONObject();
                    x.put("id", id);
                    x.put("name", rs.getString(2));
                    x.put("total_amount", rs.getDouble(3));
                    x.put("installment_amount", rs.getDouble(4));
                    x.put("total_installments", total);
                    x.put("paid_installments", paid);
                    x.put("remaining_installments", Math.max(0, total - paid));
                    x.put("active", rs.getInt(7) == 1 && paid < total);
                    x.put("next_due_date", current == null ? safe(rs.getString(8)) : current.optString("due_date"));
                    x.put("next_installment_number", current == null ? (paid < total ? paid + 1 : total) : current.optInt("installment_number"));
                    x.put("next_amount", current == null ? rs.getDouble(4) : current.optDouble("amount", rs.getDouble(4)));
                    x.put("last_paid_date", safe(rs.getString(9)));
                    x.put("completed_at", safe(rs.getString(10)));
                    String due = x.optString("next_due_date", "");
                    String status = paid >= total ? "completed" : (due.isEmpty() ? "needs_due_date" : (due.compareTo(today()) < 0 ? "overdue" : "pending"));
                    x.put("current_status", status);
                    rows.put(x);
                }
            }
        }
        JSONObject out = new JSONObject(); out.put("ok", true); out.put("rows", rows); return out;
    }

    private JSONObject add(JSONObject c, JSONObject f) throws Exception {
        String name = f.optString("name", "").trim();
        BigDecimal totalAmount = money(f.optString("totalAmount", "0"));
        BigDecimal installmentAmount = money(f.optString("installmentAmount", "0"));
        int total = f.optInt("totalInstallments", 0);
        int paid = f.optInt("paidInstallments", 0);
        String nextDue = f.optString("nextDueDate", "").trim();
        if (name.isEmpty()) throw new IllegalArgumentException("Informe o nome do financiamento.");
        if (total < 1) throw new IllegalArgumentException("Informe a quantidade total de parcelas.");
        if (paid < 0 || paid > total) throw new IllegalArgumentException("Parcelas pagas inválidas.");
        if (installmentAmount.compareTo(BigDecimal.ZERO) <= 0) throw new IllegalArgumentException("Informe o valor da parcela.");
        if (paid < total && nextDue.isEmpty()) throw new IllegalArgumentException("Informe o vencimento da próxima parcela.");
        if (!nextDue.isEmpty()) validateDate(nextDue);

        String p = prefix(c);
        try (Connection conn = open(c)) {
            ensureSchema(conn, p);
            conn.setAutoCommit(false);
            try {
                long id;
                try (PreparedStatement ps = conn.prepareStatement(
                    "INSERT INTO " + p + "financings(name,total_amount,installment_amount,total_installments,paid_installments,active,next_due_date) VALUES(?,?,?,?,?,?,?)",
                    Statement.RETURN_GENERATED_KEYS)) {
                    ps.setString(1, name); ps.setBigDecimal(2, totalAmount); ps.setBigDecimal(3, installmentAmount);
                    ps.setInt(4, total); ps.setInt(5, paid); ps.setInt(6, paid < total ? 1 : 0);
                    if (nextDue.isEmpty()) ps.setNull(7, java.sql.Types.DATE); else ps.setString(7, nextDue);
                    ps.executeUpdate();
                    try (ResultSet keys = ps.getGeneratedKeys()) { if (!keys.next()) throw new IllegalStateException("Não foi possível identificar o financiamento criado."); id = keys.getLong(1); }
                }
                if (paid < total) generatePendingSchedule(conn, p, id, paid, total, installmentAmount, nextDue);
                if (paid >= total) {
                    try (PreparedStatement ps = conn.prepareStatement("UPDATE " + p + "financings SET completed_at=CURDATE(),active=0 WHERE id=?")) { ps.setLong(1, id); ps.executeUpdate(); }
                }
                conn.commit();
            } catch (Throwable e) { try { conn.rollback(); } catch (Throwable ignored) {} throw e; }
        }
        return ok("Financiamento cadastrado com controle de parcelas.");
    }

    private JSONObject setNextDue(JSONObject c, JSONObject f) throws Exception {
        long id = f.optLong("financingId", 0L);
        String nextDue = f.optString("nextDueDate", "").trim();
        if (id <= 0) throw new IllegalArgumentException("Financiamento inválido.");
        validateDate(nextDue);
        String p = prefix(c);
        try (Connection conn = open(c)) {
            ensureSchema(conn, p);
            conn.setAutoCommit(false);
            try {
                FinancingBase base = financingBase(conn, p, id, true);
                if (base == null) throw new IllegalArgumentException("Financiamento não encontrado.");
                if (base.paid >= base.total) throw new IllegalStateException("Este financiamento já está concluído.");
                try (PreparedStatement del = conn.prepareStatement("DELETE FROM " + p + "financing_installments WHERE financing_id=? AND status<>'paid'")) { del.setLong(1, id); del.executeUpdate(); }
                generatePendingSchedule(conn, p, id, base.paid, base.total, base.amount, nextDue);
                try (PreparedStatement ps = conn.prepareStatement("UPDATE " + p + "financings SET next_due_date=?,active=1,completed_at=NULL WHERE id=?")) {
                    ps.setString(1, nextDue); ps.setLong(2, id); ps.executeUpdate();
                }
                conn.commit();
            } catch (Throwable e) { try { conn.rollback(); } catch (Throwable ignored) {} throw e; }
        }
        return ok("Próxima parcela configurada.");
    }

    private JSONObject payCurrent(JSONObject c, JSONObject f) throws Exception {
        long id = f.optLong("financingId", 0L);
        String paidDate = f.optString("paidDate", today()).trim();
        if (id <= 0) throw new IllegalArgumentException("Financiamento inválido.");
        validateDate(paidDate);
        String p = prefix(c);
        JSONObject out = new JSONObject();
        try (Connection conn = open(c)) {
            ensureSchema(conn, p);
            conn.setAutoCommit(false);
            try {
                FinancingBase base = financingBase(conn, p, id, true);
                if (base == null) throw new IllegalArgumentException("Financiamento não encontrado.");
                if (base.paid >= base.total) throw new IllegalStateException("Este financiamento já está concluído.");
                if (pendingCount(conn, p, id) == 0) {
                    if (base.nextDue == null || base.nextDue.trim().isEmpty()) throw new IllegalStateException("Defina o vencimento da próxima parcela antes de marcar o pagamento.");
                    generatePendingSchedule(conn, p, id, base.paid, base.total, base.amount, base.nextDue);
                }
                JSONObject current = firstPendingForUpdate(conn, p, id);
                if (current == null) throw new IllegalStateException("Não há parcela pendente para este financiamento.");
                long installmentId = current.getLong("id");
                int installmentNo = current.getInt("installment_number");
                try (PreparedStatement ps = conn.prepareStatement("UPDATE " + p + "financing_installments SET status='paid',paid_date=? WHERE id=? AND status<>'paid'")) {
                    ps.setString(1, paidDate); ps.setLong(2, installmentId);
                    if (ps.executeUpdate() != 1) throw new IllegalStateException("A parcela já foi atualizada. Reabra a tela e tente novamente.");
                }
                int newPaid = Math.min(base.total, Math.max(base.paid + 1, installmentNo));
                JSONObject next = firstPending(conn, p, id);
                if (newPaid >= base.total || next == null) {
                    try (PreparedStatement ps = conn.prepareStatement("UPDATE " + p + "financings SET paid_installments=?,last_paid_date=?,next_due_date=NULL,active=0,completed_at=? WHERE id=?")) {
                        ps.setInt(1, base.total); ps.setString(2, paidDate); ps.setString(3, paidDate); ps.setLong(4, id); ps.executeUpdate();
                    }
                    out.put("completed", true); out.put("message", "Parcela " + installmentNo + "/" + base.total + " paga. Financiamento concluído.");
                } else {
                    String nextDue = next.optString("due_date");
                    try (PreparedStatement ps = conn.prepareStatement("UPDATE " + p + "financings SET paid_installments=?,last_paid_date=?,next_due_date=?,active=1 WHERE id=?")) {
                        ps.setInt(1, newPaid); ps.setString(2, paidDate); ps.setString(3, nextDue); ps.setLong(4, id); ps.executeUpdate();
                    }
                    out.put("completed", false); out.put("next_installment_number", next.optInt("installment_number")); out.put("next_due_date", nextDue);
                    out.put("message", "Parcela " + installmentNo + "/" + base.total + " paga. Próxima: " + next.optInt("installment_number") + "/" + base.total + ".");
                }
                conn.commit();
            } catch (Throwable e) { try { conn.rollback(); } catch (Throwable ignored) {} throw e; }
        }
        out.put("ok", true); out.put("paid_date", paidDate); return out;
    }

    private JSONObject installments(JSONObject c, JSONObject f) throws Exception {
        long id = f.optLong("financingId", 0L);
        if (id <= 0) throw new IllegalArgumentException("Financiamento inválido.");
        String p = prefix(c);
        JSONArray rows = new JSONArray();
        int legacyPaid = 0;
        try (Connection conn = open(c)) {
            ensureSchema(conn, p);
            FinancingBase base = financingBase(conn, p, id, false);
            if (base == null) throw new IllegalArgumentException("Financiamento não encontrado.");
            int detailedPaid = 0;
            try (PreparedStatement ps = conn.prepareStatement("SELECT id,installment_number,due_date,amount,status,paid_date FROM " + p + "financing_installments WHERE financing_id=? ORDER BY installment_number ASC")) {
                ps.setLong(1, id);
                try (ResultSet rs = ps.executeQuery()) {
                    while (rs.next()) {
                        JSONObject x = new JSONObject(); x.put("id", rs.getLong(1)); x.put("installment_number", rs.getInt(2)); x.put("due_date", safe(rs.getString(3)));
                        x.put("amount", rs.getDouble(4)); x.put("status", rs.getString(5)); x.put("paid_date", safe(rs.getString(6)));
                        if ("paid".equalsIgnoreCase(rs.getString(5))) detailedPaid++;
                        rows.put(x);
                    }
                }
            }
            legacyPaid = Math.max(0, base.paid - detailedPaid);
        }
        JSONObject out = new JSONObject(); out.put("ok", true); out.put("rows", rows); out.put("legacy_paid_count", legacyPaid); return out;
    }

    private void ensureSchema(Connection conn, String p) throws Exception {
        try (Statement st = conn.createStatement()) {
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "financings (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(160) NOT NULL,total_amount DECIMAL(14,2) NOT NULL DEFAULT 0,installment_amount DECIMAL(14,2) NOT NULL DEFAULT 0,total_installments INT NOT NULL DEFAULT 0,paid_installments INT NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "financing_installments (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,financing_id BIGINT UNSIGNED NOT NULL,installment_number INT NOT NULL,due_date DATE NOT NULL,amount DECIMAL(14,2) NOT NULL DEFAULT 0,status VARCHAR(20) NOT NULL DEFAULT 'pending',paid_date DATE NULL,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),UNIQUE KEY uq_financing_installment(financing_id,installment_number),KEY idx_financing_status(financing_id,status),KEY idx_financing_due(due_date)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
        }
        ensureColumn(conn, p + "financings", "next_due_date", "DATE NULL");
        ensureColumn(conn, p + "financings", "last_paid_date", "DATE NULL");
        ensureColumn(conn, p + "financings", "completed_at", "DATE NULL");
    }

    private void ensureColumn(Connection conn, String table, String column, String definition) throws Exception {
        String sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=? AND COLUMN_NAME=?";
        try (PreparedStatement ps = conn.prepareStatement(sql)) {
            ps.setString(1, table); ps.setString(2, column);
            try (ResultSet rs = ps.executeQuery()) { if (rs.next() && rs.getInt(1) > 0) return; }
        }
        try (Statement st = conn.createStatement()) { st.execute("ALTER TABLE " + table + " ADD COLUMN " + column + " " + definition); }
    }

    private void generatePendingSchedule(Connection conn, String p, long id, int paid, int total, BigDecimal amount, String nextDue) throws Exception {
        validateDate(nextDue);
        try (PreparedStatement ps = conn.prepareStatement("INSERT IGNORE INTO " + p + "financing_installments(financing_id,installment_number,due_date,amount,status) VALUES(?,?,?,?, 'pending')")) {
            for (int n = paid + 1; n <= total; n++) {
                ps.setLong(1, id); ps.setInt(2, n); ps.setString(3, addMonths(nextDue, n - (paid + 1))); ps.setBigDecimal(4, amount); ps.addBatch();
            }
            ps.executeBatch();
        }
    }

    private JSONObject firstPending(Connection conn, String p, long id) throws Exception {
        String sql = "SELECT id,installment_number,due_date,amount FROM " + p + "financing_installments WHERE financing_id=? AND status<>'paid' ORDER BY installment_number ASC LIMIT 1";
        try (PreparedStatement ps = conn.prepareStatement(sql)) { ps.setLong(1, id); try (ResultSet rs = ps.executeQuery()) { return rs.next() ? installmentJson(rs) : null; } }
    }

    private JSONObject firstPendingForUpdate(Connection conn, String p, long id) throws Exception {
        String sql = "SELECT id,installment_number,due_date,amount FROM " + p + "financing_installments WHERE financing_id=? AND status<>'paid' ORDER BY installment_number ASC LIMIT 1 FOR UPDATE";
        try (PreparedStatement ps = conn.prepareStatement(sql)) { ps.setLong(1, id); try (ResultSet rs = ps.executeQuery()) { return rs.next() ? installmentJson(rs) : null; } }
    }

    private JSONObject installmentJson(ResultSet rs) throws Exception {
        JSONObject x = new JSONObject(); x.put("id", rs.getLong(1)); x.put("installment_number", rs.getInt(2)); x.put("due_date", rs.getString(3)); x.put("amount", rs.getDouble(4)); return x;
    }

    private int pendingCount(Connection conn, String p, long id) throws Exception {
        try (PreparedStatement ps = conn.prepareStatement("SELECT COUNT(*) FROM " + p + "financing_installments WHERE financing_id=? AND status<>'paid'")) { ps.setLong(1, id); try (ResultSet rs = ps.executeQuery()) { return rs.next() ? rs.getInt(1) : 0; } }
    }

    private FinancingBase financingBase(Connection conn, String p, long id, boolean lock) throws Exception {
        String sql = "SELECT total_installments,paid_installments,installment_amount,next_due_date FROM " + p + "financings WHERE id=?" + (lock ? " FOR UPDATE" : "");
        try (PreparedStatement ps = conn.prepareStatement(sql)) {
            ps.setLong(1, id);
            try (ResultSet rs = ps.executeQuery()) {
                if (!rs.next()) return null;
                return new FinancingBase(rs.getInt(1), rs.getInt(2), rs.getBigDecimal(3), rs.getString(4));
            }
        }
    }

    private Connection open(JSONObject c) throws Exception {
        String host = c.optString("host", "").trim(); int port = c.optInt("port", 3306); String database = c.optString("database", "").trim();
        String user = c.optString("user", "").trim(); String password = c.optString("password", ""); boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) throw new IllegalArgumentException("Configuração MySQL incompleta.");
        StringBuilder url = new StringBuilder("jdbc:mariadb://").append(host).append(':').append(port).append('/').append(database).append("?connectTimeout=5000&socketTimeout=7000&tcpKeepAlive=true");
        if (ssl) url.append("&useSsl=true&trustServerCertificate=true"); else url.append("&useSsl=false");
        Properties props = new Properties(); props.setProperty("user", user); props.setProperty("password", password);
        UrlParser parser = UrlParser.parse(url.toString(), props); if (parser == null || parser.getHostAddresses() == null) throw new IllegalArgumentException("Connection string MySQL inválida.");
        return MariaDbConnection.newConnection(parser, null);
    }

    private static BigDecimal money(String raw) {
        String v = raw == null ? "0" : raw.trim().replace("R$", "").replace(" ", "");
        if (v.contains(",")) v = v.replace(".", "").replace(',', '.');
        if (v.isEmpty()) v = "0";
        return new BigDecimal(v);
    }

    private static void validateDate(String value) {
        if (value == null || !value.matches("20\\d{2}-\\d{2}-\\d{2}")) throw new IllegalArgumentException("Data inválida.");
        try { java.sql.Date.valueOf(value); } catch (Throwable e) { throw new IllegalArgumentException("Data inválida."); }
    }

    private static String addMonths(String iso, int months) throws Exception {
        SimpleDateFormat fmt = new SimpleDateFormat("yyyy-MM-dd", Locale.US); fmt.setLenient(false);
        Date d = fmt.parse(iso); Calendar c = Calendar.getInstance(); c.setTime(d); int day = c.get(Calendar.DAY_OF_MONTH); c.set(Calendar.DAY_OF_MONTH, 1); c.add(Calendar.MONTH, months);
        int max = c.getActualMaximum(Calendar.DAY_OF_MONTH); c.set(Calendar.DAY_OF_MONTH, Math.min(day, max)); return fmt.format(c.getTime());
    }

    private static String today() { return new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new Date()); }
    private static String safe(String s) { return s == null ? "" : s; }
    private static String prefix(JSONObject c) { String p = c.optString("prefix", "granaok_"); return p.matches("[A-Za-z0-9_]{1,32}") ? p : "granaok_"; }
    private static JSONObject ok(String message) throws Exception { JSONObject o = new JSONObject(); o.put("ok", true); o.put("message", message); return o; }
    private static JSONObject fail(String message) { JSONObject o = new JSONObject(); try { o.put("ok", false); o.put("error", message); } catch (Throwable ignored) {} return o; }
    private static String clean(Throwable e) { String m = e == null ? "Falha desconhecida" : e.getMessage(); if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName(); m = m.replaceAll("(?i)password=[^&\\s]+", "password=***"); return m.length() > 500 ? m.substring(0, 500) : m; }
    private static void send(ResultReceiver receiver, JSONObject payload) { if (receiver == null) return; try { Bundle b = new Bundle(); b.putString("json", payload == null ? "{}" : payload.toString()); receiver.send(CODE_RESULT, b); } catch (Throwable ignored) {} }

    private static final class FinancingBase {
        final int total, paid; final BigDecimal amount; final String nextDue;
        FinancingBase(int total, int paid, BigDecimal amount, String nextDue) { this.total = total; this.paid = paid; this.amount = amount; this.nextDue = nextDue; }
    }
}
