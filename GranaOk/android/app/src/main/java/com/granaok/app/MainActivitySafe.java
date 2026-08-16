package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.ResultReceiver;
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

import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.util.Locale;
import java.util.concurrent.Executor;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.PBEKeySpec;

public class MainActivitySafe extends FragmentActivity {
    private static final String PREFS = "granaok_secure";
    private static final String KEY_ALIAS = "granaok_db_key";

    private WebView webView;
    private SharedPreferences securePrefs;

    private interface DbResultHandler {
        void handle(JSONObject result) throws Exception;
    }

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
        if (webView != null) {
            try {
                webView.stopLoading();
                webView.removeJavascriptInterface("GranaNative");
                webView.destroy();
            } catch (Throwable ignored) {
            }
        }
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
            startDbRequest(
                DbWorkerService.ACTION_TEST,
                configJson,
                null,
                "GranaOkDbTest",
                null
            );
        }

        @JavascriptInterface
        public void installDatabase(String configJson, String adminJson) {
            startDbRequest(
                DbWorkerService.ACTION_INSTALL,
                configJson,
                adminJson,
                "GranaOkInstallResult",
                result -> {
                    if (!result.optBoolean("ok", false)) return;
                    JSONObject c = new JSONObject(configJson);
                    JSONObject a = new JSONObject(adminJson);
                    saveDbConfig(c);
                    saveLocalLogin(a.optString("email"), a.optString("loginPassword"));
                    setAdminPasswordInternal(a.optString("configPassword"));
                }
            );
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
            try {
                startDbRequest(
                    DbWorkerService.ACTION_DASHBOARD,
                    readDbConfig().toString(),
                    null,
                    "GranaOkDashboard",
                    null
                );
            } catch (Throwable e) {
                callbackError("GranaOkDashboard", "config", e);
            }
        }

        @JavascriptInterface
        public void loadTransactions() {
            try {
                startDbRequest(
                    DbWorkerService.ACTION_TRANSACTIONS,
                    readDbConfig().toString(),
                    null,
                    "GranaOkTransactions",
                    null
                );
            } catch (Throwable e) {
                callbackError("GranaOkTransactions", "config", e);
            }
        }

        @JavascriptInterface
        public void addTransaction(String transactionJson) {
            try {
                startDbRequest(
                    DbWorkerService.ACTION_ADD_TRANSACTION,
                    readDbConfig().toString(),
                    transactionJson,
                    "GranaOkTransactionSaved",
                    null
                );
            } catch (Throwable e) {
                callbackError("GranaOkTransactionSaved", "config", e);
            }
        }

        @JavascriptInterface
        public void authenticate() {
            runOnUiThread(MainActivitySafe.this::showBiometricPrompt);
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
            return saved != null && MessageDigest.isEqual(
                saved.getBytes(StandardCharsets.UTF_8),
                sha256(password).getBytes(StandardCharsets.UTF_8)
            );
        }

        @JavascriptInterface
        public void clearConfiguration() {
            securePrefs.edit()
                .remove("db_cipher").remove("db_iv")
                .remove("login_email").remove("login_salt").remove("login_hash")
                .apply();
        }
    }

    private void startDbRequest(
        String action,
        String configJson,
        String payloadJson,
        String finalCallback,
        DbResultHandler handler
    ) {
        try {
            ResultReceiver receiver = new ResultReceiver(new Handler(Looper.getMainLooper())) {
                @Override
                protected void onReceiveResult(int resultCode, Bundle resultData) {
                    String json = resultData == null ? "{}" : resultData.getString("json", "{}");
                    JSONObject out;
                    try {
                        out = new JSONObject(json);
                    } catch (Throwable e) {
                        out = new JSONObject();
                        try {
                            out.put("ok", false);
                            out.put("stage", "ipc");
                            out.put("error", "Resposta inválida do processo MySQL.");
                        } catch (Throwable ignored) {
                        }
                    }

                    if (resultCode == DbWorkerService.CODE_PROGRESS) {
                        callback("GranaOkDbProgress", out);
                        return;
                    }

                    if (handler != null) {
                        try {
                            handler.handle(out);
                        } catch (Throwable e) {
                            try {
                                out.put("ok", false);
                                out.put("stage", "local-security");
                                out.put("error", cleanThrowable(e));
                            } catch (Throwable ignored) {
                            }
                        }
                    }
                    callback(finalCallback, out);
                }
            };

            Intent i = new Intent(this, DbWorkerService.class);
            i.putExtra(DbWorkerService.EXTRA_ACTION, action);
            i.putExtra(DbWorkerService.EXTRA_CONFIG, configJson == null ? "{}" : configJson);
            if (payloadJson != null) i.putExtra(DbWorkerService.EXTRA_PAYLOAD, payloadJson);
            i.putExtra(DbWorkerService.EXTRA_RECEIVER, receiver);
            startService(i);
        } catch (Throwable e) {
            callbackError(finalCallback, "ipc-start", e);
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
        if (ks.containsAlias(KEY_ALIAS)) {
            return ((KeyStore.SecretKeyEntry) ks.getEntry(KEY_ALIAS, null)).getSecretKey();
        }
        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
        generator.init(new KeyGenParameterSpec.Builder(
            KEY_ALIAS,
            KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT
        ).setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .build());
        return generator.generateKey();
    }

    private void saveLocalLogin(String email, String password) throws Exception {
        if (email == null || email.trim().isEmpty() || password == null || password.length() < 4) {
            throw new IllegalArgumentException("E-mail e senha de login são obrigatórios.");
        }
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

    private static String cleanThrowable(Throwable e) {
        String m = e == null ? "Falha desconhecida" : e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        m = m.replaceAll("(?i)password=[^&\\s]+", "password=***");
        m = m.replaceAll("(?i)(password|senha)[=:]\\s*[^\\s,;]+", "$1=***");
        return m.length() > 420 ? m.substring(0, 420) : m;
    }

    private void callbackError(String fn, String stage, Throwable e) {
        JSONObject out = new JSONObject();
        try {
            out.put("ok", false);
            out.put("stage", stage);
            out.put("error", cleanThrowable(e));
        } catch (Throwable ignored) {
        }
        callback(fn, out);
    }

    private void callback(String fn, JSONObject payload) {
        if (fn == null || fn.trim().isEmpty()) return;
        final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(payload == null ? "{}" : payload.toString()) + ")";
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
                    if (webView != null) {
                        webView.evaluateJavascript("window.GranaOkNativeAuth && window.GranaOkNativeAuth(true, '')", null);
                    }
                }

                @Override
                public void onAuthenticationError(int errorCode, CharSequence errString) {
                    super.onAuthenticationError(errorCode, errString);
                    String msg = errString.toString().replace("'", "\\'");
                    if (webView != null) {
                        webView.evaluateJavascript("window.GranaOkNativeAuth && window.GranaOkNativeAuth(false, '" + msg + "')", null);
                    }
                }
            });

        BiometricPrompt.PromptInfo info = new BiometricPrompt.PromptInfo.Builder()
            .setTitle("Desbloquear GranaOk")
            .setSubtitle("Use biometria ou o bloqueio do aparelho")
            .setAllowedAuthenticators(
                BiometricManager.Authenticators.BIOMETRIC_STRONG |
                BiometricManager.Authenticators.DEVICE_CREDENTIAL
            )
            .build();
        prompt.authenticate(info);
    }
}
