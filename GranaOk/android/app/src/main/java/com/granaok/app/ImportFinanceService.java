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
import java.security.MessageDigest;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.util.Locale;
import java.util.Properties;
import java.util.concurrent.atomic.AtomicBoolean;

/** Importa lançamentos revisados de extratos sem executar JDBC no processo da interface. */
public class ImportFinanceService extends Service {
    public static final String EXTRA_CONFIG = "import_config";
    public static final String EXTRA_PAYLOAD = "import_payload";
    public static final String EXTRA_RECEIVER = "import_receiver";
    private static final long HARD_TIMEOUT_MS = 30000L;
    private static final int CODE_RESULT = 2;
    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    @Override public IBinder onBind(Intent intent) { return null; }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null) { stopSelf(startId); return START_NOT_STICKY; }
        final String configJson = intent.getStringExtra(EXTRA_CONFIG);
        final String payloadJson = intent.getStringExtra(EXTRA_PAYLOAD);
        final ResultReceiver receiver = intent.getParcelableExtra(EXTRA_RECEIVER);
        final AtomicBoolean delivered = new AtomicBoolean(false);
        final Runnable timeout = () -> {
            if (delivered.compareAndSet(false, true)) send(receiver, fail("A importação excedeu 30 segundos e foi interrompida."));
            stopSelf(startId);
            mainHandler.postDelayed(() -> Process.killProcess(Process.myPid()), 250L);
        };
        mainHandler.postDelayed(timeout, HARD_TIMEOUT_MS);

        Thread worker = new Thread(() -> {
            JSONObject out;
            try {
                JSONObject config = new JSONObject(configJson == null ? "{}" : configJson);
                JSONObject payload = new JSONObject(payloadJson == null ? "{}" : payloadJson);
                out = importRows(config, payload);
            } catch (Throwable e) {
                out = fail(cleanThrowable(e));
            }
            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, out);
                stopSelf(startId);
            }
        }, "GranaOk-Import-Worker");
        worker.setDaemon(true); worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject importRows(JSONObject c, JSONObject payload) throws Exception {
        String p = prefix(c);
        JSONArray rows = payload.optJSONArray("rows");
        if (rows == null || rows.length() == 0) throw new IllegalArgumentException("Nenhum lançamento selecionado para importar.");
        long accountIdValue = payload.optLong("accountId", 0);
        Long accountId = accountIdValue > 0 ? accountIdValue : null;
        String fileName = payload.optString("fileName", "extrato").trim();
        String source = payload.optString("source", "bank_statement").trim();
        if (!source.matches("[A-Za-z0-9_-]{1,40}")) source = "bank_statement";
        int imported = 0, skipped = 0, invalid = 0;

        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            conn.setAutoCommit(false);
            try {
                for (int i = 0; i < rows.length(); i++) {
                    JSONObject r = rows.optJSONObject(i); if (r == null) { invalid++; continue; }
                    String date = r.optString("date", "").trim();
                    String description = r.optString("description", "").trim();
                    String type = "income".equals(r.optString("type", "expense")) ? "income" : "expense";
                    BigDecimal amount = DocumentParser.parseFlexibleMoney(String.valueOf(r.opt("amount")));
                    if (!date.matches("20\\d{2}-\\d{2}-\\d{2}") || description.isEmpty() || amount == null || amount.compareTo(BigDecimal.ZERO) <= 0) { invalid++; continue; }
                    String external = r.optString("external_id", "").trim();
                    String observation = r.optString("observations", "").trim();
                    String fingerprint = fingerprint(source, accountId, external, date, type, amount, description);
                    if (existsHash(conn, p, fingerprint)) { skipped++; continue; }
                    Long categoryId = categoryId(conn, p, "Extrato bancário", type);
                    String notes = "Importado de " + fileName;
                    if (!observation.isEmpty()) notes += " · " + observation;
                    String sql = "INSERT INTO " + p + "transactions(account_id,category_id,type,description,amount,due_date,paid_date,status,source,observations,external_id,import_hash,import_source) VALUES(?,?,?,?,?,?,?,'paid','statement_import',?,?,?,?)";
                    try (PreparedStatement ps = conn.prepareStatement(sql)) {
                        if (accountId == null) ps.setNull(1, java.sql.Types.BIGINT); else ps.setLong(1, accountId);
                        if (categoryId == null) ps.setNull(2, java.sql.Types.BIGINT); else ps.setLong(2, categoryId);
                        ps.setString(3, type); ps.setString(4, truncate(description, 255)); ps.setBigDecimal(5, amount.abs());
                        ps.setString(6, date); ps.setString(7, date); ps.setString(8, truncate(notes, 1000));
                        if (external.isEmpty()) ps.setNull(9, java.sql.Types.VARCHAR); else ps.setString(9, truncate(external, 180));
                        ps.setString(10, fingerprint); ps.setString(11, source); ps.executeUpdate();
                    }
                    imported++;
                }
                conn.commit();
            } catch (Throwable e) {
                conn.rollback(); throw e;
            } finally { conn.setAutoCommit(true); }
        }
        JSONObject out = new JSONObject(); out.put("ok", true); out.put("imported", imported); out.put("skipped", skipped); out.put("invalid", invalid);
        out.put("message", imported + " lançamento(s) importado(s)." + (skipped > 0 ? " " + skipped + " duplicado(s) ignorado(s)." : ""));
        return out;
    }

    private void ensureTables(Connection conn, String p) throws Exception {
        try (Statement st = conn.createStatement()) {
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "categories (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,kind VARCHAR(20) NOT NULL DEFAULT 'expense',active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),UNIQUE KEY uq_cat(name,kind)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "transactions (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,account_id BIGINT UNSIGNED NULL,category_id BIGINT UNSIGNED NULL,type VARCHAR(20) NOT NULL,description VARCHAR(255) NOT NULL,amount DECIMAL(14,2) NOT NULL,due_date DATE NOT NULL,paid_date DATE NULL,status VARCHAR(20) NOT NULL DEFAULT 'pending',source VARCHAR(30) NOT NULL DEFAULT 'manual',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_due(due_date),KEY idx_type(type),KEY idx_cat(category_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
        }
        addColumn(conn, p + "transactions", "observations", "TEXT NULL");
        addColumn(conn, p + "transactions", "external_id", "VARCHAR(180) NULL");
        addColumn(conn, p + "transactions", "import_hash", "CHAR(64) NULL");
        addColumn(conn, p + "transactions", "import_source", "VARCHAR(40) NULL");
        try (Statement st = conn.createStatement()) { st.execute("CREATE INDEX idx_import_hash ON " + p + "transactions(import_hash)"); }
        catch (java.sql.SQLException e) { if (!(e.getErrorCode() == 1061 || "42000".equals(e.getSQLState()))) throw e; }
    }

    private Long categoryId(Connection conn, String p, String name, String kind) throws Exception {
        try (PreparedStatement ins = conn.prepareStatement("INSERT IGNORE INTO " + p + "categories(name,kind,active) VALUES(?,?,1)")) {
            ins.setString(1, name); ins.setString(2, kind); ins.executeUpdate();
        }
        try (PreparedStatement ps = conn.prepareStatement("SELECT id FROM " + p + "categories WHERE name=? AND kind=? LIMIT 1")) {
            ps.setString(1, name); ps.setString(2, kind); try (ResultSet rs = ps.executeQuery()) { return rs.next() ? rs.getLong(1) : null; }
        }
    }

    private boolean existsHash(Connection conn, String p, String hash) throws Exception {
        try (PreparedStatement ps = conn.prepareStatement("SELECT id FROM " + p + "transactions WHERE import_hash=? LIMIT 1")) {
            ps.setString(1, hash); try (ResultSet rs = ps.executeQuery()) { return rs.next(); }
        }
    }

    private static String fingerprint(String source, Long accountId, String external, String date, String type, BigDecimal amount, String description) throws Exception {
        String base = source + "|" + (accountId == null ? "0" : accountId) + "|";
        if (external != null && !external.trim().isEmpty()) base += "id:" + external.trim();
        else base += date + "|" + type + "|" + amount.abs().setScale(2, BigDecimal.ROUND_HALF_UP).toPlainString() + "|" + description.trim().toLowerCase(Locale.ROOT).replaceAll("\\s+", " ");
        MessageDigest md = MessageDigest.getInstance("SHA-256"); byte[] bytes = md.digest(base.getBytes("UTF-8"));
        StringBuilder out = new StringBuilder(); for (byte b : bytes) out.append(String.format(Locale.US, "%02x", b & 0xff)); return out.toString();
    }

    private static void addColumn(Connection conn, String table, String column, String definition) throws Exception {
        try (Statement st = conn.createStatement()) { st.execute("ALTER TABLE " + table + " ADD COLUMN " + column + " " + definition); }
        catch (java.sql.SQLException e) { if (!("42S21".equals(e.getSQLState()) || e.getErrorCode() == 1060)) throw e; }
    }

    private Connection openConnection(JSONObject c) throws Exception {
        String host = c.optString("host", "").trim(); int port = c.optInt("port", 3306); String database = c.optString("database", "").trim();
        String user = c.optString("user", "").trim(); String password = c.optString("password", ""); boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) throw new IllegalArgumentException("Configuração MySQL incompleta.");
        String url = "jdbc:mariadb://" + host + ":" + port + "/" + database + "?connectTimeout=5000&socketTimeout=12000&tcpKeepAlive=true" + (ssl ? "&useSsl=true&trustServerCertificate=true" : "&useSsl=false");
        Properties props = new Properties(); props.setProperty("user", user); props.setProperty("password", password);
        UrlParser parser = UrlParser.parse(url, props); if (parser == null || parser.getHostAddresses() == null) throw new IllegalArgumentException("Connection string MySQL inválida.");
        return MariaDbConnection.newConnection(parser, null);
    }

    private static String prefix(JSONObject c) { String p = c.optString("prefix", "granaok_"); return p.matches("[A-Za-z0-9_]{1,32}") ? p : "granaok_"; }
    private static String truncate(String s, int max) { return s == null ? "" : (s.length() <= max ? s : s.substring(0, max)); }
    private static JSONObject fail(String message) { JSONObject o = new JSONObject(); try { o.put("ok", false); o.put("error", message); } catch (Throwable ignored) {} return o; }
    private static String cleanThrowable(Throwable e) { String m = e == null ? "Falha desconhecida" : e.getMessage(); if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName(); return truncate(m.replaceAll("(?i)password=[^&\\s]+", "password=***"), 500); }
    private static void send(ResultReceiver receiver, JSONObject payload) { if (receiver == null) return; try { Bundle b = new Bundle(); b.putString("json", payload == null ? "{}" : payload.toString()); receiver.send(CODE_RESULT, b); } catch (Throwable ignored) {} }
}
