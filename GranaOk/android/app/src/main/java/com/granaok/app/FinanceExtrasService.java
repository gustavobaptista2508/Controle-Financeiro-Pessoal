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
import java.math.RoundingMode;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.Locale;
import java.util.Properties;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicBoolean;

public class FinanceExtrasService extends Service {
    public static final String EXTRA_ACTION = "extras_action";
    public static final String EXTRA_CONFIG = "extras_config";
    public static final String EXTRA_PAYLOAD = "extras_payload";
    public static final String EXTRA_RECEIVER = "extras_receiver";

    public static final String ACTION_LIST_CATEGORIES = "list_categories";
    public static final String ACTION_ADD_CATEGORY = "add_category";
    public static final String ACTION_ADD_EXPENSE = "add_expense";
    public static final String ACTION_LIST_ACCOUNTS = "list_accounts_plus";
    public static final String ACTION_ADD_ACCOUNT = "add_account_plus";

    private static final int CODE_RESULT = 2;
    private static final long HARD_TIMEOUT_MS = 18000L;
    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    private static final String[][] DEFAULT_CATEGORIES = new String[][]{
        {"expense","Moradia"},{"expense","Alimentação"},{"expense","Supermercado"},
        {"expense","Transporte"},{"expense","Combustível"},{"expense","Saúde"},
        {"expense","Farmácia"},{"expense","Educação"},{"expense","Lazer"},
        {"expense","Internet"},{"expense","Telefone"},{"expense","Energia"},
        {"expense","Água"},{"expense","Assinaturas"},{"expense","Financiamentos"},
        {"expense","Empréstimos"},{"expense","Impostos"},{"expense","Manutenção"},
        {"expense","Vestuário"},{"expense","Outros"},
        {"income","Salário"},{"income","Bonificação"},{"income","Renda extra"},
        {"income","Freelance"},{"income","Reembolso"},{"income","Rendimentos"},
        {"income","Venda"},{"income","Outros"}
    };

    @Override
    public IBinder onBind(Intent intent) { return null; }

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
                switch (action == null ? "" : action) {
                    case ACTION_LIST_CATEGORIES: out = listCategories(config, payload); break;
                    case ACTION_ADD_CATEGORY: out = addCategory(config, payload); break;
                    case ACTION_ADD_EXPENSE: out = addExpense(config, payload); break;
                    case ACTION_LIST_ACCOUNTS: out = listAccounts(config); break;
                    case ACTION_ADD_ACCOUNT: out = addAccount(config, payload); break;
                    default: out = fail("Operação complementar desconhecida.");
                }
            } catch (Throwable e) {
                out = fail(cleanThrowable(e));
            }
            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, out);
                stopSelf(startId);
            }
        }, "GranaOk-Extras-Worker");
        worker.setDaemon(true);
        worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject listCategories(JSONObject c, JSONObject payload) throws Exception {
        String p = prefix(c);
        String kind = payload.optString("kind", "expense");
        if (!kind.equals("income")) kind = "expense";
        JSONArray rows = new JSONArray();
        try (Connection conn = openConnection(c)) {
            ensureCore(conn, p);
            seedCategories(conn, p);
            try (PreparedStatement ps = conn.prepareStatement("SELECT id,name,kind,active FROM " + p + "categories WHERE kind=? AND active=1 ORDER BY name ASC")) {
                ps.setString(1, kind);
                try (ResultSet rs = ps.executeQuery()) {
                    while (rs.next()) {
                        JSONObject x = new JSONObject();
                        x.put("id", rs.getLong(1));
                        x.put("name", rs.getString(2));
                        x.put("kind", rs.getString(3));
                        x.put("active", rs.getInt(4) == 1);
                        rows.put(x);
                    }
                }
            }
        }
        JSONObject out = ok(); out.put("rows", rows); return out;
    }

    private JSONObject addCategory(JSONObject c, JSONObject payload) throws Exception {
        String p = prefix(c);
        String name = payload.optString("name", "").trim();
        String kind = payload.optString("kind", "expense");
        if (name.isEmpty()) throw new IllegalArgumentException("Informe o nome da categoria.");
        if (!kind.equals("income")) kind = "expense";
        try (Connection conn = openConnection(c)) {
            ensureCore(conn, p);
            seedCategories(conn, p);
            try (PreparedStatement ps = conn.prepareStatement("INSERT IGNORE INTO " + p + "categories(name,kind,active) VALUES(?,?,1)")) {
                ps.setString(1, name); ps.setString(2, kind); ps.executeUpdate();
            }
        }
        JSONObject out = ok(); out.put("message", "Categoria cadastrada."); return out;
    }

    private JSONObject addExpense(JSONObject c, JSONObject payload) throws Exception {
        String p = prefix(c);
        String description = payload.optString("description", "").trim();
        String category = payload.optString("category", "Outros").trim();
        String dueDate = payload.optString("dueDate", today()).trim();
        int installments = payload.optInt("installments", 1);
        BigDecimal total = decimal(payload.optString("totalAmount", "0"));
        Long accountId = payload.has("accountId") && payload.optLong("accountId", 0) > 0 ? payload.optLong("accountId") : null;

        if (description.isEmpty()) throw new IllegalArgumentException("Informe a descrição da despesa.");
        if (total.compareTo(BigDecimal.ZERO) <= 0) throw new IllegalArgumentException("Informe um valor maior que zero.");
        if (installments < 1 || installments > 120) throw new IllegalArgumentException("O parcelamento deve ter entre 1 e 120 parcelas.");

        try (Connection conn = openConnection(c)) {
            ensureCore(conn, p);
            seedCategories(conn, p);
            ensureInstallmentColumns(conn, p);
            Long categoryId = categoryId(conn, p, category, "expense");
            String groupId = installments > 1 ? UUID.randomUUID().toString() : null;
            BigDecimal base = total.divide(new BigDecimal(installments), 2, RoundingMode.DOWN);
            BigDecimal allocated = BigDecimal.ZERO;
            Calendar first = parseDate(dueDate);

            conn.setAutoCommit(false);
            try {
                String sql = "INSERT INTO " + p + "transactions (account_id,category_id,type,description,amount,due_date,status,source,installment_group,installment_number,installment_total) VALUES (?,?, 'expense', ?, ?, ?, 'pending', ?, ?, ?, ?)";
                for (int i = 1; i <= installments; i++) {
                    BigDecimal amount = i == installments ? total.subtract(allocated) : base;
                    allocated = allocated.add(amount);
                    Calendar date = (Calendar) first.clone();
                    date.add(Calendar.MONTH, i - 1);
                    String label = installments > 1 ? description + " (" + i + "/" + installments + ")" : description;
                    try (PreparedStatement ps = conn.prepareStatement(sql)) {
                        if (accountId == null) ps.setNull(1, java.sql.Types.BIGINT); else ps.setLong(1, accountId);
                        if (categoryId == null) ps.setNull(2, java.sql.Types.BIGINT); else ps.setLong(2, categoryId);
                        ps.setString(3, label);
                        ps.setBigDecimal(4, amount);
                        ps.setString(5, new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(date.getTime()));
                        ps.setString(6, installments > 1 ? "installment" : "manual");
                        if (groupId == null) ps.setNull(7, java.sql.Types.VARCHAR); else ps.setString(7, groupId);
                        ps.setInt(8, i);
                        ps.setInt(9, installments);
                        ps.executeUpdate();
                    }
                }
                conn.commit();
            } catch (Throwable e) {
                conn.rollback();
                throw e;
            } finally {
                conn.setAutoCommit(true);
            }
        }

        JSONObject out = ok();
        out.put("message", installments > 1 ? "Despesa parcelada em " + installments + " vezes." : "Despesa cadastrada.");
        out.put("installments", installments);
        return out;
    }

    private JSONObject listAccounts(JSONObject c) throws Exception {
        String p = prefix(c);
        JSONArray rows = new JSONArray();
        try (Connection conn = openConnection(c)) {
            ensureCore(conn, p);
            ensureBankCode(conn, p);
            try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery("SELECT id,name,type,initial_balance,current_balance,active,COALESCE(bank_code,'other') FROM " + p + "accounts ORDER BY active DESC,name ASC")) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("id", rs.getLong(1)); x.put("name", rs.getString(2)); x.put("type", rs.getString(3));
                    x.put("initial_balance", rs.getDouble(4)); x.put("current_balance", rs.getDouble(5));
                    x.put("active", rs.getInt(6) == 1); x.put("bank_code", rs.getString(7)); rows.put(x);
                }
            }
        }
        JSONObject out = ok(); out.put("rows", rows); return out;
    }

    private JSONObject addAccount(JSONObject c, JSONObject payload) throws Exception {
        String p = prefix(c);
        String name = payload.optString("name", "").trim();
        String type = payload.optString("type", "checking").trim();
        String bankCode = payload.optString("bankCode", "other").trim().toLowerCase(Locale.ROOT);
        BigDecimal initial = decimal(payload.optString("initialBalance", "0"));
        BigDecimal current = decimal(payload.optString("currentBalance", initial.toPlainString()));
        if (name.isEmpty()) throw new IllegalArgumentException("Informe o nome da conta.");
        if (!type.matches("[A-Za-z0-9_-]{1,30}")) type = "bank";
        if (!bankCode.matches("[a-z0-9_-]{1,30}")) bankCode = "other";
        try (Connection conn = openConnection(c)) {
            ensureCore(conn, p); ensureBankCode(conn, p);
            try (PreparedStatement ps = conn.prepareStatement("INSERT INTO " + p + "accounts(name,type,initial_balance,current_balance,active,bank_code) VALUES(?,?,?,?,1,?)")) {
                ps.setString(1, name); ps.setString(2, type); ps.setBigDecimal(3, initial); ps.setBigDecimal(4, current); ps.setString(5, bankCode); ps.executeUpdate();
            }
        }
        JSONObject out = ok(); out.put("message", "Conta cadastrada."); return out;
    }

    private void ensureCore(Connection conn, String p) throws Exception {
        try (Statement st = conn.createStatement()) {
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "categories (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,kind VARCHAR(20) NOT NULL DEFAULT 'expense',active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),UNIQUE KEY uq_cat(name,kind)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "accounts (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,type VARCHAR(40) NOT NULL DEFAULT 'bank',initial_balance DECIMAL(14,2) NOT NULL DEFAULT 0,current_balance DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_person(person_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "transactions (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,account_id BIGINT UNSIGNED NULL,category_id BIGINT UNSIGNED NULL,type VARCHAR(20) NOT NULL,description VARCHAR(255) NOT NULL,amount DECIMAL(14,2) NOT NULL,due_date DATE NOT NULL,paid_date DATE NULL,status VARCHAR(20) NOT NULL DEFAULT 'pending',source VARCHAR(30) NOT NULL DEFAULT 'manual',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_due(due_date),KEY idx_type(type),KEY idx_cat(category_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
        }
    }

    private void seedCategories(Connection conn, String p) throws Exception {
        try (PreparedStatement ps = conn.prepareStatement("INSERT IGNORE INTO " + p + "categories(name,kind,active) VALUES(?,?,1)")) {
            for (String[] row : DEFAULT_CATEGORIES) {
                ps.setString(1, row[1]); ps.setString(2, row[0]); ps.addBatch();
            }
            ps.executeBatch();
        }
    }

    private Long categoryId(Connection conn, String p, String name, String kind) throws Exception {
        if (name == null || name.trim().isEmpty()) name = "Outros";
        try (PreparedStatement ins = conn.prepareStatement("INSERT IGNORE INTO " + p + "categories(name,kind,active) VALUES(?,?,1)")) {
            ins.setString(1, name); ins.setString(2, kind); ins.executeUpdate();
        }
        try (PreparedStatement ps = conn.prepareStatement("SELECT id FROM " + p + "categories WHERE name=? AND kind=? LIMIT 1")) {
            ps.setString(1, name); ps.setString(2, kind);
            try (ResultSet rs = ps.executeQuery()) { return rs.next() ? rs.getLong(1) : null; }
        }
    }

    private void ensureInstallmentColumns(Connection conn, String p) throws Exception {
        addColumn(conn, p + "transactions", "installment_group", "VARCHAR(64) NULL");
        addColumn(conn, p + "transactions", "installment_number", "INT NOT NULL DEFAULT 1");
        addColumn(conn, p + "transactions", "installment_total", "INT NOT NULL DEFAULT 1");
    }

    private void ensureBankCode(Connection conn, String p) throws Exception {
        addColumn(conn, p + "accounts", "bank_code", "VARCHAR(40) NULL");
    }

    private void addColumn(Connection conn, String table, String column, String definition) throws Exception {
        try (Statement st = conn.createStatement()) {
            st.execute("ALTER TABLE " + table + " ADD COLUMN " + column + " " + definition);
        } catch (java.sql.SQLException e) {
            String state = e.getSQLState();
            if (!("42S21".equals(state) || e.getErrorCode() == 1060)) throw e;
        }
    }

    private Connection openConnection(JSONObject c) throws Exception {
        String host = c.optString("host", "").trim();
        int port = c.optInt("port", 3306);
        String database = c.optString("database", "").trim();
        String user = c.optString("user", "").trim();
        String password = c.optString("password", "");
        boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) throw new IllegalArgumentException("Configuração MySQL incompleta.");
        StringBuilder url = new StringBuilder("jdbc:mariadb://").append(host).append(':').append(port).append('/').append(database).append("?connectTimeout=5000&socketTimeout=7000&tcpKeepAlive=true");
        if (ssl) url.append("&useSsl=true&trustServerCertificate=true"); else url.append("&useSsl=false");
        Properties props = new Properties(); props.setProperty("user", user); props.setProperty("password", password);
        UrlParser parser = UrlParser.parse(url.toString(), props);
        if (parser == null || parser.getHostAddresses() == null) throw new IllegalArgumentException("Connection string MySQL inválida.");
        return MariaDbConnection.newConnection(parser, null);
    }

    private static Calendar parseDate(String value) throws Exception {
        Date d = new SimpleDateFormat("yyyy-MM-dd", Locale.US).parse(value);
        Calendar c = Calendar.getInstance(); c.setTime(d == null ? new Date() : d); return c;
    }
    private static String today() { return new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new Date()); }
    private static BigDecimal decimal(String value) {
        String v = value == null ? "0" : value.trim().replace(".", "").replace(',', '.');
        if (v.isEmpty()) v = "0"; return new BigDecimal(v);
    }
    private static String prefix(JSONObject c) {
        String p = c.optString("prefix", "granaok_"); return p.matches("[A-Za-z0-9_]{1,32}") ? p : "granaok_";
    }
    private static JSONObject ok() throws Exception { JSONObject o = new JSONObject(); o.put("ok", true); return o; }
    private static JSONObject fail(String m) { JSONObject o = new JSONObject(); try { o.put("ok", false); o.put("error", m); } catch (Throwable ignored) {} return o; }
    private static String cleanThrowable(Throwable e) {
        String m = e == null ? "Falha desconhecida" : e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        m = m.replaceAll("(?i)password=[^&\\s]+", "password=***").replaceAll("(?i)(password|senha)[=:]\\s*[^\\s,;]+", "$1=***");
        return m.length() > 500 ? m.substring(0, 500) : m;
    }
    private static void send(ResultReceiver r, JSONObject p) {
        if (r == null) return; try { Bundle b = new Bundle(); b.putString("json", p == null ? "{}" : p.toString()); r.send(CODE_RESULT, b); } catch (Throwable ignored) {}
    }
}
