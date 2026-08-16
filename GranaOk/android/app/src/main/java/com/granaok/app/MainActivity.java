package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.SharedPreferences;
import android.os.Bundle;
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

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.concurrent.Executor;

public class MainActivity extends FragmentActivity {
    private WebView webView;
    private SharedPreferences securePrefs;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_SECURE, WindowManager.LayoutParams.FLAG_SECURE);
        securePrefs = getSharedPreferences("granaok_secure", MODE_PRIVATE);

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

    public class AndroidBridge {
        @JavascriptInterface
        public boolean isNative() { return true; }

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
            if (password == null || password.length() < 4) return;
            securePrefs.edit().putString("admin_password_hash", sha256(password)).apply();
        }

        @JavascriptInterface
        public boolean verifyAdminPassword(String password) {
            if (password == null) return false;
            String saved = securePrefs.getString("admin_password_hash", null);
            return saved != null && constantTimeEquals(saved, sha256(password));
        }
    }

    private static boolean constantTimeEquals(String a, String b) {
        byte[] x = a.getBytes(StandardCharsets.UTF_8);
        byte[] y = b.getBytes(StandardCharsets.UTF_8);
        return MessageDigest.isEqual(x, y);
    }

    private static String sha256(String value) {
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(value.getBytes(StandardCharsets.UTF_8));
            StringBuilder hex = new StringBuilder();
            for (byte b : hash) hex.append(String.format("%02x", b));
            return hex.toString();
        } catch (NoSuchAlgorithmException e) {
            throw new IllegalStateException("SHA-256 indisponível", e);
        }
    }

    private void showBiometricPrompt() {
        Executor executor = ContextCompat.getMainExecutor(this);
        BiometricPrompt prompt = new BiometricPrompt(this, executor,
            new BiometricPrompt.AuthenticationCallback() {
                @Override
                public void onAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) {
                    super.onAuthenticationSucceeded(result);
                    webView.evaluateJavascript("window.GranaOkNativeAuth && window.GranaOkNativeAuth(true, '')", null);
                }

                @Override
                public void onAuthenticationError(int errorCode, CharSequence errString) {
                    super.onAuthenticationError(errorCode, errString);
                    String msg = errString.toString().replace("'", "\\'");
                    webView.evaluateJavascript("window.GranaOkNativeAuth && window.GranaOkNativeAuth(false, '" + msg + "')", null);
                }
            });

        BiometricPrompt.PromptInfo info = new BiometricPrompt.PromptInfo.Builder()
            .setTitle("Desbloquear GranaOk")
            .setSubtitle("Use biometria ou o bloqueio do aparelho")
            .setAllowedAuthenticators(
                BiometricManager.Authenticators.BIOMETRIC_STRONG |
                BiometricManager.Authenticators.DEVICE_CREDENTIAL)
            .build();
        prompt.authenticate(info);
    }
}
