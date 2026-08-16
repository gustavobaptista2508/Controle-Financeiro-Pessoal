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
import java.net.InetSocketAddress;
import java.net.Socket;
import java.sql.Connection;
import java.sql.DatabaseMetaData;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.Properties;
import java.util.concurrent.atomic.AtomicBoolean;

public class DbWorkerService extends Service {
    public static final String EXTRA_ACTION = "db_action";
    public static final String EXTRA_CONFIG = "db_config";
    public static final String EXTRA_PAYLOAD = "db_payload";
    public static final String EXTRA_RECEIVER = "db_receiver";

    public static final String ACTION_TEST = "test";
    public static final String ACTION_INSTALL = "install";
    public static final String ACTION_DASHBOARD = "dashboard";
    public static final String ACTION_TRANSACTIONS = "transactions";
    public static final String ACTION_ADD_TRANSACTION = "add_transaction";

    public static final int CODE_PROGRESS = 1;
    public static final int CODE_RESULT = 2;
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
                JSONObject out = new JSONObject();
                try {
                    out.put("ok", false);
                    out.put("stage", "timeout");
                    out.put("error", "A operação MySQL excedeu 18 segundos. O processo de banco foi encerrado sem congelar o GranaOk.");
                } catch (Throwable ignored) {
                }
                send(receiver, CODE_RESULT, out);
            }
            stopSelf(startId);
            mainHandler.postDelayed(() -> Process.killProcess(Process.myPid()), 250L);
        };
        mainHandler.postDelayed(timeout, HARD_TIMEOUT_MS);

        Thread worker = new Thread(() -> {
            JSONObject out;
            try {
                JSONObject config = new JSONObject(configJson == null ? "{}" : configJson);
                switch (action == null ? "" : action) {
                    case ACTION_TEST:
                        out = testConnection(config, receiver);
                        break;
                    case ACTION_INSTALL:
                        out = install(config, new JSONObject(payloadJson == null ? "{}" : payloadJson), receiver);
                        break;
                    case ACTION_DASHBOARD:
                        out = buildDashboard(config);
                        break;
                    case ACTION_TRANSACTIONS:
                        out = buildTransactions(config);
                        break;
                    case ACTION_ADD_TRANSACTION:
                        out = addTransaction(config, new JSONObject(payloadJson == null ? "{}" : payloadJson));
                        break;
                    default:
                        out = fail("action", "Operação de banco desconhecida.");
                }
            } catch (Throwable e) {
                out = fail("worker", cleanThrowable(e));
            }

            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, CODE_RESULT, out);
                stopSelf(startId);
            }
        }, "GranaOk-MySQL-Worker");
        worker.setDaemon(true);
        worker.start();

        return START_NOT_STICKY;
    }

    private JSONObject testConnection(JSONObject c, ResultReceiver receiver) throws Exception {
        String host = c.optString("host", "").trim();
        int port = c.optInt("port", 3306);
        progress(receiver, "tcp", "Verificando DNS, rede e porta " + port, host);
        preflightTcp(host, port);

        progress(receiver, "driver", "Inicializando conector MySQL compatível com Android", host);
        progress(receiver, "mysql", "Autenticando no MySQL", host);
        try (Connection conn = openConnection(c)) {
            DatabaseMetaData meta = conn.getMetaData();
            JSONObject out = new JSONObject();
            out.put("ok", true);
            out.put("stage", "done");
            out.put("product", meta.getDatabaseProductName());
            out.put("version", meta.getDatabaseProductVersion());
            out.put("driver", "MariaDB Connector/J 2.7.13 · conexão direta");
            out.put("database", c.optString("database"));
            out.put("host", host);
            out.put("port", port);
            out.put("user", c.optString("user"));
            try (Statement st = conn.createStatement();
                 ResultSet rs = st.executeQuery("SELECT CURRENT_USER(), USER(), DATABASE(), @@version")) {
                if (rs.next()) {
                    out.put("currentUser", rs.getString(1));
                    out.put("loginUser", rs.getString(2));
                    out.put("currentDatabase", rs.getString(3));
                    out.put("serverVersion", rs.getString(4));
                }
            }
            return out;
        }
    }

    private JSONObject install(JSONObject c, JSONObject admin, ResultReceiver receiver) throws Exception {
        String prefix = sanitizePrefix(c.optString("prefix", "granaok_"));
        c.put("prefix", prefix);
        String host = c.optString("host", "").trim();
        int port = c.optInt("port", 3306);
        progress(receiver, "tcp", "Verificando acesso ao servidor", host);
        preflightTcp(host, port);
        progress(receiver, "mysql", "Criando estrutura do GranaOk", host);
        try (Connection conn = openConnection(c)) {
            createSchema(conn, prefix);
            seedAdmin(conn, prefix, admin.optString("name", "Administrador"));
        }
        JSONObject out = new JSONObject();
        out.put("ok", true);
        out.put("message", "Banco conectado e estrutura GranaOk criada.");
        return out;
    }

    private JSONObject addTransaction(JSONObject c, JSONObject tx) {
        JSONObject out = new JSONObject();
        try {
            String prefix = sanitizePrefix(c.optString("prefix", "granaok_"));
            try (Connection conn = openConnection(c)) {
                Long categoryId = ensureCategory(conn, prefix, tx.optString("category", "Outros"), tx.optString("type", "expense"));
                String sql = "INSERT INTO " + prefix + "transactions (category_id,type,description,amount,due_date,status,source) VALUES (?,?,?,?,?,'pending','manual')";
                try (PreparedStatement ps = conn.prepareStatement(sql)) {
                    if (categoryId == null) ps.setNull(1, java.sql.Types.BIGINT); else ps.setLong(1, categoryId);
                    ps.setString(2, tx.optString("type", "expense"));
                    ps.setString(3, tx.optString("description", "Lançamento"));
                    ps.setBigDecimal(4, new BigDecimal(tx.optString("amount", "0").replace(',', '.')));
                    ps.setString(5, tx.optString("dueDate", new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new Date())));
                    ps.executeUpdate();
                }
            }
            out.put("ok", true);
        } catch (Throwable e) {
            try {
                out.put("ok", false);
                out.put("error", cleanThrowable(e));
            } catch (Throwable ignored) {
            }
        }
        return out;
    }

    private JSONObject buildDashboard(JSONObject c) {
        JSONObject out = new JSONObject();
        try {
            String p = sanitizePrefix(c.optString("prefix", "granaok_"));
            try (Connection conn = openConnection(c)) {
                double income = scalar(conn, "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='income' AND DATE_FORMAT(due_date,'%Y-%m')=DATE_FORMAT(CURDATE(),'%Y-%m')");
                double expenses = scalar(conn, "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='expense' AND source<>'card_invoice' AND DATE_FORMAT(due_date,'%Y-%m')=DATE_FORMAT(CURDATE(),'%Y-%m')");
                double invoices = scalar(conn, "SELECT COALESCE(SUM(amount),0) FROM " + p + "card_invoices WHERE DATE_FORMAT(due_date,'%Y-%m')=DATE_FORMAT(CURDATE(),'%Y-%m')");
                double prevIncome = scalar(conn, "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='income' AND DATE_FORMAT(due_date,'%Y-%m')=DATE_FORMAT(DATE_SUB(CURDATE(),INTERVAL 1 MONTH),'%Y-%m')");
                double prevExpenses = scalar(conn, "SELECT COALESCE(SUM(amount),0) FROM " + p + "transactions WHERE type='expense' AND source<>'card_invoice' AND DATE_FORMAT(due_date,'%Y-%m')=DATE_FORMAT(DATE_SUB(CURDATE(),INTERVAL 1 MONTH),'%Y-%m')");
                out.put("ok", true);
                out.put("income", income);
                out.put("expenses", expenses);
                out.put("card_invoices", invoices);
                out.put("projected_balance", income - expenses - invoices);
                out.put("prev_income", prevIncome);
                out.put("prev_expenses", prevExpenses);

                JSONArray cats = new JSONArray();
                String qCats = "SELECT COALESCE(c.name,'Sem categoria'),COALESCE(SUM(t.amount),0) total FROM " + p + "transactions t LEFT JOIN " + p + "categories c ON c.id=t.category_id WHERE t.type='expense' AND t.source<>'card_invoice' AND DATE_FORMAT(t.due_date,'%Y-%m')=DATE_FORMAT(CURDATE(),'%Y-%m') GROUP BY c.name ORDER BY total DESC LIMIT 8";
                try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(qCats)) {
                    while (rs.next()) {
                        JSONObject x = new JSONObject();
                        x.put("name", rs.getString(1));
                        x.put("total", rs.getDouble(2));
                        cats.put(x);
                    }
                }
                out.put("categories", cats);

                JSONArray upcoming = new JSONArray();
                String qUp = "SELECT description,due_date,amount,type FROM " + p + "transactions WHERE status<>'paid' AND due_date BETWEEN CURDATE() AND DATE_ADD(CURDATE(),INTERVAL 14 DAY) ORDER BY due_date ASC LIMIT 10";
                try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(qUp)) {
                    while (rs.next()) {
                        JSONObject x = new JSONObject();
                        x.put("description", rs.getString(1));
                        x.put("due_date", rs.getString(2));
                        x.put("amount", rs.getDouble(3));
                        x.put("type", rs.getString(4));
                        upcoming.put(x);
                    }
                }
                out.put("upcoming", upcoming);
            }
        } catch (Throwable e) {
            try {
                out.put("ok", false);
                out.put("error", cleanThrowable(e));
            } catch (Throwable ignored) {
            }
        }
        return out;
    }

    private JSONObject buildTransactions(JSONObject c) {
        JSONObject out = new JSONObject();
        try {
            String p = sanitizePrefix(c.optString("prefix", "granaok_"));
            JSONArray rows = new JSONArray();
            try (Connection conn = openConnection(c);
                 Statement st = conn.createStatement();
                 ResultSet rs = st.executeQuery("SELECT t.id,t.type,t.description,t.amount,t.due_date,t.status,COALESCE(c.name,'Sem categoria') FROM " + p + "transactions t LEFT JOIN " + p + "categories c ON c.id=t.category_id WHERE DATE_FORMAT(t.due_date,'%Y-%m')=DATE_FORMAT(CURDATE(),'%Y-%m') ORDER BY t.due_date DESC,t.id DESC LIMIT 80")) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("id", rs.getLong(1));
                    x.put("type", rs.getString(2));
                    x.put("description", rs.getString(3));
                    x.put("amount", rs.getDouble(4));
                    x.put("due_date", rs.getString(5));
                    x.put("status", rs.getString(6));
                    x.put("category", rs.getString(7));
                    rows.put(x);
                }
            }
            out.put("ok", true);
            out.put("rows", rows);
        } catch (Throwable e) {
            try {
                out.put("ok", false);
                out.put("error", cleanThrowable(e));
            } catch (Throwable ignored) {
            }
        }
        return out;
    }

    private Connection openConnection(JSONObject c) throws Exception {
        String host = c.optString("host", "").trim();
        int port = c.optInt("port", 3306);
        String database = c.optString("database", "").trim();
        String user = c.optString("user", "").trim();
        String password = c.optString("password", "");
        boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) {
            throw new IllegalArgumentException("Host, banco e usuário são obrigatórios.");
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

    private static void preflightTcp(String host, int port) throws Exception {
        if (host == null || host.trim().isEmpty()) throw new IllegalArgumentException("Host do MySQL não informado.");
        if (port < 1 || port > 65535) throw new IllegalArgumentException("Porta MySQL inválida.");
        try (Socket socket = new Socket()) {
            socket.connect(new InetSocketAddress(host.trim(), port), 3500);
        }
    }

    private void createSchema(Connection conn, String p) throws Exception {
        String[] sql = new String[]{
            "CREATE TABLE IF NOT EXISTS " + p + "people (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "accounts (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,type VARCHAR(40) NOT NULL DEFAULT 'bank',initial_balance DECIMAL(14,2) NOT NULL DEFAULT 0,current_balance DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_person(person_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "categories (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,kind VARCHAR(20) NOT NULL DEFAULT 'expense',active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),UNIQUE KEY uq_cat(name,kind)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "transactions (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,account_id BIGINT UNSIGNED NULL,category_id BIGINT UNSIGNED NULL,type VARCHAR(20) NOT NULL,description VARCHAR(255) NOT NULL,amount DECIMAL(14,2) NOT NULL,due_date DATE NOT NULL,paid_date DATE NULL,status VARCHAR(20) NOT NULL DEFAULT 'pending',source VARCHAR(30) NOT NULL DEFAULT 'manual',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_due(due_date),KEY idx_type(type),KEY idx_cat(category_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "cards (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,closing_day INT NOT NULL DEFAULT 1,due_day INT NOT NULL DEFAULT 10,limit_amount DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "card_invoices (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,card_id BIGINT UNSIGNED NOT NULL,reference_month DATE NOT NULL,due_date DATE NOT NULL,amount DECIMAL(14,2) NOT NULL DEFAULT 0,status VARCHAR(20) NOT NULL DEFAULT 'open',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_invoice_due(due_date)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "financings (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(160) NOT NULL,total_amount DECIMAL(14,2) NOT NULL DEFAULT 0,installment_amount DECIMAL(14,2) NOT NULL DEFAULT 0,total_installments INT NOT NULL DEFAULT 0,paid_installments INT NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "reserves (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(160) NOT NULL,amount DECIMAL(14,2) NOT NULL DEFAULT 0,target_amount DECIMAL(14,2) NOT NULL DEFAULT 0,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "settings (setting_key VARCHAR(120) NOT NULL,setting_value TEXT NULL,updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,PRIMARY KEY(setting_key)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            "CREATE TABLE IF NOT EXISTS " + p + "ai_messages (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,role VARCHAR(20) NOT NULL,message TEXT NOT NULL,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"
        };
        try (Statement st = conn.createStatement()) {
            for (String q : sql) st.execute(q);
        }
    }

    private void seedAdmin(Connection conn, String p, String name) throws Exception {
        try (PreparedStatement chk = conn.prepareStatement("SELECT id FROM " + p + "people WHERE name=? LIMIT 1")) {
            chk.setString(1, name);
            try (ResultSet rs = chk.executeQuery()) {
                if (rs.next()) return;
            }
        }
        try (PreparedStatement ins = conn.prepareStatement("INSERT INTO " + p + "people(name) VALUES(?)")) {
            ins.setString(1, name);
            ins.executeUpdate();
        }
    }

    private Long ensureCategory(Connection conn, String p, String name, String kind) throws Exception {
        String n = (name == null || name.trim().isEmpty()) ? "Outros" : name.trim();
        try (PreparedStatement ps = conn.prepareStatement("SELECT id FROM " + p + "categories WHERE name=? AND kind=? LIMIT 1")) {
            ps.setString(1, n);
            ps.setString(2, kind);
            try (ResultSet rs = ps.executeQuery()) {
                if (rs.next()) return rs.getLong(1);
            }
        }
        try (PreparedStatement ps = conn.prepareStatement("INSERT INTO " + p + "categories(name,kind) VALUES(?,?)", Statement.RETURN_GENERATED_KEYS)) {
            ps.setString(1, n);
            ps.setString(2, kind);
            ps.executeUpdate();
            try (ResultSet rs = ps.getGeneratedKeys()) {
                if (rs.next()) return rs.getLong(1);
            }
        }
        return null;
    }

    private double scalar(Connection conn, String sql) throws Exception {
        try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
            return rs.next() ? rs.getDouble(1) : 0d;
        }
    }

    private static String sanitizePrefix(String prefix) {
        if (prefix == null || !prefix.matches("[A-Za-z0-9_]{1,32}")) return "granaok_";
        return prefix;
    }

    private static JSONObject fail(String stage, String message) {
        JSONObject out = new JSONObject();
        try {
            out.put("ok", false);
            out.put("stage", stage);
            out.put("error", message);
        } catch (Throwable ignored) {
        }
        return out;
    }

    private static String cleanThrowable(Throwable e) {
        String m = e == null ? "Falha desconhecida" : e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        Throwable cause = e == null ? null : e.getCause();
        if (cause != null && cause != e) {
            String cm = cause.getMessage();
            if (cm != null && !cm.trim().isEmpty() && !m.contains(cm)) m += " · " + cm;
        }
        m = m.replaceAll("(?i)password=[^&\\s]+", "password=***");
        m = m.replaceAll("(?i)(password|senha)[=:]\\s*[^\\s,;]+", "$1=***");
        return m.length() > 500 ? m.substring(0, 500) : m;
    }

    private static void progress(ResultReceiver receiver, String stage, String message, String host) {
        JSONObject out = new JSONObject();
        try {
            out.put("stage", stage);
            out.put("message", message);
            out.put("host", host);
        } catch (Throwable ignored) {
        }
        send(receiver, CODE_PROGRESS, out);
    }

    private static void send(ResultReceiver receiver, int code, JSONObject payload) {
        if (receiver == null) return;
        try {
            Bundle b = new Bundle();
            b.putString("json", payload == null ? "{}" : payload.toString());
            receiver.send(code, b);
        } catch (Throwable ignored) {
        }
    }
}
