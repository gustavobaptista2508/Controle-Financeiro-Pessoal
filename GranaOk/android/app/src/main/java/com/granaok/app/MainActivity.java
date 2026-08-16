package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import android.view.WindowManager;
import android.webkit.JavascriptInterface;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import androidx.biometric.BiometricManager;
import androidx.biometric.BiometricPrompt;
import androidx.core.content.ContextCompat;
import androidx.fragment.app.FragmentActivity;

import org.json.JSONArray;
import org.json.JSONObject;

import java.math.BigDecimal;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.sql.Connection;
import java.sql.DatabaseMetaData;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.concurrent.Executor;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.PBEKeySpec;

public class MainActivity extends FragmentActivity {
    private static final String PREFS = "granaok_secure";
    private static final String KEY_ALIAS = "granaok_db_key";
    private WebView webView;
    private SharedPreferences securePrefs;
    private final ExecutorService dbExecutor = Executors.newSingleThreadExecutor(r -> {
        Thread t = new Thread(r, "GranaOk-DB");
        t.setUncaughtExceptionHandler((thread, error) -> {
            try {
                JSONObject out = new JSONObject();
                out.put("ok", false);
                out.put("stage", "runtime");
                out.put("error", cleanThrowable(error));
                callback("GranaOkDbTest", out);
            } catch (Throwable ignored) {
            }
        });
        return t;
    });

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_SECURE, WindowManager.LayoutParams.FLAG_SECURE);
        securePrefs = getSharedPreferences(PREFS, MODE_PRIVATE);

        webView = new WebView(this);
        setContentView(webView);

        WebSettings s = webView.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true);
        s.setDatabaseEnabled(true);
        s.setAllowFileAccess(true);
        s.setAllowContentAccess(false);
        s.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        s.setSupportMultipleWindows(false);

        webView.addJavascriptInterface(new AndroidBridge(), "GranaNative");
        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                String url = request.getUrl().toString();
                return !url.startsWith("file:///android_asset/www/");
            }
        });
        webView.loadUrl("file:///android_asset/www/index.html");
    }

    @Override
    protected void onDestroy() {
        dbExecutor.shutdownNow();
        if (webView != null) webView.destroy();
        super.onDestroy();
    }

    public class AndroidBridge {
        @JavascriptInterface
        public boolean isNative() {
            return true;
        }

        @JavascriptInterface
        public boolean hasDatabaseConfig() {
            return securePrefs.contains("db_cipher") && securePrefs.contains("db_iv");
        }

        @JavascriptInterface
        public boolean hasLocalLogin() {
            return securePrefs.contains("login_hash") && securePrefs.contains("login_email");
        }

        @JavascriptInterface
        public String getDatabaseInfo() {
            try {
                JSONObject c = readDbConfig();
                JSONObject safe = new JSONObject();
                safe.put("name", c.optString("name", "ProjetoGranaOK"));
                safe.put("host", c.optString("host"));
                safe.put("port", c.optInt("port", 3306));
                safe.put("database", c.optString("database"));
                safe.put("user", c.optString("user"));
                safe.put("ssl", c.optBoolean("ssl", false));
                safe.put("prefix", c.optString("prefix", "granaok_"));
                return safe.toString();
            } catch (Throwable e) {
                return "{}";
            }
        }

        @JavascriptInterface
        public void testDatabase(String configJson) {
            dbExecutor.execute(() -> {
                JSONObject out = new JSONObject();
                try {
                    JSONObject c = new JSONObject(configJson);
                    String host = c.optString("host", "").trim();
                    int port = c.optInt("port", 3306);

                    out.put("stage", "tcp");
                    preflightTcp(host, port);

                    out.put("stage", "driver");
                    Class.forName("org.mariadb.jdbc.Driver");

                    out.put("stage", "mysql");
                    try (Connection conn = openConnection(c, false)) {
                        DatabaseMetaData meta = conn.getMetaData();
                        out.put("ok", true);
                        out.put("stage", "done");
                        out.put("product", meta.getDatabaseProductName());
                        out.put("version", meta.getDatabaseProductVersion());
                        out.put("driver", meta.getDriverName() + " " + meta.getDriverVersion());
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
                    }
                } catch (Throwable e) {
                    try {
                        out.put("ok", false);
                        if (!out.has("stage")) out.put("stage", "unknown");
                        out.put("error", cleanThrowable(e));
                    } catch (Throwable ignored) {
                    }
                }
                safeCallback("GranaOkDbTest", out);
            });
        }

        @JavascriptInterface
        public void installDatabase(String configJson, String adminJson) {
            dbExecutor.execute(() -> {
                JSONObject out = new JSONObject();
                try {
                    JSONObject c = new JSONObject(configJson);
                    JSONObject a = new JSONObject(adminJson);
                    String prefix = sanitizePrefix(c.optString("prefix", "granaok_"));
                    c.put("prefix", prefix);
                    preflightTcp(c.optString("host").trim(), c.optInt("port", 3306));
                    try (Connection conn = openConnection(c, true)) {
                        createSchema(conn, prefix);
                        seedAdmin(conn, prefix, a.optString("name", "Administrador"));
                    }
                    saveDbConfig(c);
                    saveLocalLogin(a.optString("email"), a.optString("loginPassword"));
                    setAdminPasswordInternal(a.optString("configPassword"));
                    out.put("ok", true);
                    out.put("message", "Banco conectado e estrutura GranaOk criada.");
                } catch (Throwable e) {
                    try {
                        out.put("ok", false);
                        out.put("error", cleanThrowable(e));
                    } catch (Throwable ignored) {
                    }
                }
                safeCallback("GranaOkInstallResult", out);
            });
        }

        @JavascriptInterface
        public boolean verifyLocalLogin(String email, String password) {
            try {
                String savedEmail = securePrefs.getString("login_email", "");
                String saltB64 = securePrefs.getString("login_salt", "");
                String hashB64 = securePrefs.getString("login_hash", "");
                if (!savedEmail.equalsIgnoreCase(email == null ? "" : email.trim())) return false;
                byte[] salt = Base64.decode(saltB64, Base64.NO_WRAP);
                byte[] expected = Base64.decode(hashB64, Base64.NO_WRAP);
                byte[] actual = pbkdf2(password == null ? "" : password, salt);
                return MessageDigest.isEqual(expected, actual);
            } catch (Throwable e) {
                return false;
            }
        }

        @JavascriptInterface
        public String getLocalUserEmail() {
            return securePrefs.getString("login_email", "");
        }

        @JavascriptInterface
        public void loadDashboard() {
            dbExecutor.execute(() -> safeCallback("GranaOkDashboard", buildDashboard()));
        }

        @JavascriptInterface
        public void loadTransactions() {
            dbExecutor.execute(() -> safeCallback("GranaOkTransactions", buildTransactions()));
        }

        @JavascriptInterface
        public void addTransaction(String transactionJson) {
            dbExecutor.execute(() -> {
                JSONObject out = new JSONObject();
                try {
                    JSONObject tx = new JSONObject(transactionJson);
                    JSONObject c = readDbConfig();
                    String prefix = sanitizePrefix(c.optString("prefix", "granaok_"));
                    try (Connection conn = openConnection(c, true)) {
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
                safeCallback("GranaOkTransactionSaved", out);
            });
        }

        @JavascriptInterface
        public void authenticate() {
            runOnUiThread(MainActivity.this::showBiometricPrompt);
        }

        @JavascriptInterface
        public boolean hasAdminPassword() {
            return securePrefs.contains("admin_password_hash");
        }

        @JavascriptInterface
        public void setAdminPassword(String password) {
            setAdminPasswordInternal(password);
        }

        @JavascriptInterface
        public boolean verifyAdminPassword(String password) {
            if (password == null) return false;
            String saved = securePrefs.getString("admin_password_hash", null);
            return saved != null && MessageDigest.isEqual(saved.getBytes(StandardCharsets.UTF_8), sha256(password).getBytes(StandardCharsets.UTF_8));
        }

        @JavascriptInterface
        public void clearConfiguration() {
            securePrefs.edit()
                .remove("db_cipher").remove("db_iv")
                .remove("login_email").remove("login_salt").remove("login_hash")
                .apply();
        }
    }

    private static void preflightTcp(String host, int port) throws Exception {
        if (host == null || host.trim().isEmpty()) throw new IllegalArgumentException("Host do MySQL não informado.");
        if (port < 1 || port > 65535) throw new IllegalArgumentException("Porta MySQL inválida.");
        try (Socket socket = new Socket()) {
            socket.connect(new InetSocketAddress(host.trim(), port), 7000);
        }
    }

    private Connection openConnection(JSONObject c, boolean loadDriver) throws Exception {
        if (loadDriver) Class.forName("org.mariadb.jdbc.Driver");
        String host = c.optString("host").trim();
        int port = c.optInt("port", 3306);
        String database = c.optString("database").trim();
        String user = c.optString("user").trim();
        String password = c.optString("password");
        boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) throw new IllegalArgumentException("Host, banco e usuário são obrigatórios.");
        DriverManager.setLoginTimeout(10);
        StringBuilder url = new StringBuilder("jdbc:mariadb://")
            .append(host).append(':').append(port).append('/').append(database)
            .append("?connectTimeout=10000&socketTimeout=12000&tcpKeepAlive=true");
        if (ssl) url.append("&useSsl=true&trustServerCertificate=true");
        else url.append("&useSsl=false");
        return DriverManager.getConnection(url.toString(), user, password);
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

    private JSONObject buildDashboard() {
        JSONObject out = new JSONObject();
        try {
            JSONObject c = readDbConfig();
            String p = sanitizePrefix(c.optString("prefix", "granaok_"));
            try (Connection conn = openConnection(c, true)) {
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
                String qCats = "SELECT COALESCE(c.name,'Sem categoria') category,COALESCE(SUM(t.amount),0) total FROM " + p + "transactions t LEFT JOIN " + p + "categories c ON c.id=t.category_id WHERE t.type='expense' AND t.source<>'card_invoice' AND DATE_FORMAT(t.due_date,'%Y-%m')=DATE_FORMAT(CURDATE(),'%Y-%m') GROUP BY c.name ORDER BY total DESC LIMIT 8";
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

    private JSONObject buildTransactions() {
        JSONObject out = new JSONObject();
        try {
            JSONObject c = readDbConfig();
            String p = sanitizePrefix(c.optString("prefix", "granaok_"));
            JSONArray rows = new JSONArray();
            try (Connection conn = openConnection(c, true);
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

    private double scalar(Connection conn, String sql) throws Exception {
        try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(sql)) {
            return rs.next() ? rs.getDouble(1) : 0d;
        }
    }

    private void saveDbConfig(JSONObject c) throws Exception {
        SecretKey key = getOrCreateKey();
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.ENCRYPT_MODE, key);
        byte[] encrypted = cipher.doFinal(c.toString().getBytes(StandardCharsets.UTF_8));
        securePrefs.edit()
            .putString("db_iv", Base64.encodeToString(cipher.getIV(), Base64.NO_WRAP))
            .putString("db_cipher", Base64.encodeToString(encrypted, Base64.NO_WRAP))
            .apply();
    }

    private JSONObject readDbConfig() throws Exception {
        String ivB64 = securePrefs.getString("db_iv", null);
        String cipherB64 = securePrefs.getString("db_cipher", null);
        if (ivB64 == null || cipherB64 == null) throw new IllegalStateException("Banco de dados ainda não configurado.");
        SecretKey key = getOrCreateKey();
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(128, Base64.decode(ivB64, Base64.NO_WRAP)));
        byte[] plain = cipher.doFinal(Base64.decode(cipherB64, Base64.NO_WRAP));
        return new JSONObject(new String(plain, StandardCharsets.UTF_8));
    }

    private SecretKey getOrCreateKey() throws Exception {
        KeyStore ks = KeyStore.getInstance("AndroidKeyStore");
        ks.load(null);
        if (ks.containsAlias(KEY_ALIAS)) return ((KeyStore.SecretKeyEntry) ks.getEntry(KEY_ALIAS, null)).getSecretKey();
        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
        generator.init(new KeyGenParameterSpec.Builder(KEY_ALIAS, KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .build());
        return generator.generateKey();
    }

    private void saveLocalLogin(String email, String password) throws Exception {
        if (email == null || email.trim().isEmpty() || password == null || password.length() < 4) throw new IllegalArgumentException("E-mail e senha de login são obrigatórios.");
        byte[] salt = new byte[16];
        new SecureRandom().nextBytes(salt);
        byte[] hash = pbkdf2(password, salt);
        securePrefs.edit()
            .putString("login_email", email.trim().toLowerCase(Locale.ROOT))
            .putString("login_salt", Base64.encodeToString(salt, Base64.NO_WRAP))
            .putString("login_hash", Base64.encodeToString(hash, Base64.NO_WRAP))
            .apply();
    }

    private static byte[] pbkdf2(String password, byte[] salt) throws Exception {
        PBEKeySpec spec = new PBEKeySpec(password.toCharArray(), salt, 120000, 256);
        SecretKeyFactory f = SecretKeyFactory.getInstance("PBKDF2WithHmacSHA1");
        return f.generateSecret(spec).getEncoded();
    }

    private void setAdminPasswordInternal(String password) {
        if (password == null || password.length() < 4) return;
        securePrefs.edit().putString("admin_password_hash", sha256(password)).apply();
    }

    private static String sanitizePrefix(String prefix) {
        if (prefix == null || !prefix.matches("[A-Za-z0-9_]{1,32}")) return "granaok_";
        return prefix;
    }

    private static String cleanThrowable(Throwable e) {
        String m = e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e.getClass().getSimpleName();
        Throwable cause = e.getCause();
        if (cause != null && cause != e) {
            String cm = cause.getMessage();
            if (cm != null && !cm.trim().isEmpty() && !m.contains(cm)) m += " · " + cm;
        }
        m = m.replaceAll("(?i)password=[^&\\s]+", "password=***");
        m = m.replaceAll("(?i)(password|senha)[=:]\\s*[^\\s,;]+", "$1=***");
        return m.length() > 420 ? m.substring(0, 420) : m;
    }

    private static String sha256(String value) {
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(value.getBytes(StandardCharsets.UTF_8));
            StringBuilder hex = new StringBuilder();
            for (byte b : hash) hex.append(String.format(Locale.US, "%02x", b));
            return hex.toString();
        } catch (Exception e) {
            throw new IllegalStateException("SHA-256 indisponível", e);
        }
    }

    private void safeCallback(String fn, JSONObject payload) {
        try {
            callback(fn, payload);
        } catch (Throwable ignored) {
        }
    }

    private void callback(String fn, JSONObject payload) {
        final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(payload.toString()) + ")";
        runOnUiThread(() -> {
            if (webView != null) {
                try {
                    webView.evaluateJavascript(js, null);
                } catch (Throwable ignored) {
                }
            }
        });
    }

    private void showBiometricPrompt() {
        Executor executor = ContextCompat.getMainExecutor(this);
        BiometricPrompt prompt = new BiometricPrompt(this, executor,
            new BiometricPrompt.AuthenticationCallback() {
                @Override
                public void onAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) {
                    super.onAuthenticationSucceeded(result);
                    if (webView != null) webView.evaluateJavascript("window.GranaOkNativeAuth && window.GranaOkNativeAuth(true, '')", null);
                }

                @Override
                public void onAuthenticationError(int errorCode, CharSequence errString) {
                    super.onAuthenticationError(errorCode, errString);
                    String msg = errString.toString().replace("'", "\\'");
                    if (webView != null) webView.evaluateJavascript("window.GranaOkNativeAuth && window.GranaOkNativeAuth(false, '" + msg + "')", null);
                }
            });

        BiometricPrompt.PromptInfo info = new BiometricPrompt.PromptInfo.Builder()
            .setTitle("Desbloquear GranaOk")
            .setSubtitle("Use biometria ou o bloqueio do aparelho")
            .setAllowedAuthenticators(BiometricManager.Authenticators.BIOMETRIC_STRONG | BiometricManager.Authenticators.DEVICE_CREDENTIAL)
            .build();
        prompt.authenticate(info);
    }
}
