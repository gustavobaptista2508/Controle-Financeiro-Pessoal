package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.ResultReceiver;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowInsets;
import android.view.WindowInsetsController;
import android.view.WindowManager;
import android.webkit.JavascriptInterface;
import android.webkit.WebView;

import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.security.KeyStore;

import javax.crypto.Cipher;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/** Beta 0.3.7: status bar fix + categories/installments/bank icons bridge. */
public class MainActivityV037 extends MainActivityUi {
    private static final String PREFS = "granaok_secure";
    private static final String KEY_ALIAS = "granaok_db_key";
    private WebView web;

    @SuppressLint({"SetJavaScriptEnabled", "JavascriptInterface"})
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        web = findWebView();
        forceSystemBarsVisible();
        if (web != null) {
            web.setPadding(0, 0, 0, 0);
            web.addJavascriptInterface(new ExtrasBridge(), "GranaExtras");
        }
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) forceSystemBarsVisible();
    }

    @Override
    protected void onResume() {
        super.onResume();
        forceSystemBarsVisible();
    }

    @Override
    protected void onDestroy() {
        if (web != null) {
            try { web.removeJavascriptInterface("GranaExtras"); } catch (Throwable ignored) {}
        }
        super.onDestroy();
    }

    private WebView findWebView() {
        try {
            View content = findViewById(android.R.id.content);
            if (content instanceof ViewGroup) {
                ViewGroup group = (ViewGroup) content;
                for (int i = 0; i < group.getChildCount(); i++) {
                    View child = group.getChildAt(i);
                    if (child instanceof WebView) return (WebView) child;
                }
            }
        } catch (Throwable ignored) {}
        return null;
    }

    private void forceSystemBarsVisible() {
        try {
            Window window = getWindow();
            window.clearFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN);
            window.addFlags(WindowManager.LayoutParams.FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);
            window.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE);
            window.setStatusBarColor(Color.parseColor("#F7F8FA"));
            window.setNavigationBarColor(Color.WHITE);

            int flags = 0;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) flags |= View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) flags |= View.SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR;
            window.getDecorView().setSystemUiVisibility(flags);

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                WindowInsetsController controller = window.getInsetsController();
                if (controller != null) {
                    controller.show(WindowInsets.Type.statusBars() | WindowInsets.Type.navigationBars());
                    controller.setSystemBarsBehavior(WindowInsetsController.BEHAVIOR_DEFAULT);
                    controller.setSystemBarsAppearance(
                        WindowInsetsController.APPEARANCE_LIGHT_STATUS_BARS | WindowInsetsController.APPEARANCE_LIGHT_NAVIGATION_BARS,
                        WindowInsetsController.APPEARANCE_LIGHT_STATUS_BARS | WindowInsetsController.APPEARANCE_LIGHT_NAVIGATION_BARS
                    );
                }
            }
        } catch (Throwable ignored) {}
    }

    public class ExtrasBridge {
        @JavascriptInterface
        public boolean isAvailable() { return true; }

        @JavascriptInterface
        public void loadCategories(String kind) {
            JSONObject p = new JSONObject();
            try { p.put("kind", kind); } catch (Throwable ignored) {}
            request(FinanceExtrasService.ACTION_LIST_CATEGORIES, p.toString(), "GranaOkCategories");
        }

        @JavascriptInterface
        public void addCategory(String payloadJson) {
            request(FinanceExtrasService.ACTION_ADD_CATEGORY, payloadJson, "GranaOkCategorySaved");
        }

        @JavascriptInterface
        public void addExpense(String payloadJson) {
            request(FinanceExtrasService.ACTION_ADD_EXPENSE, payloadJson, "GranaOkExpenseSaved");
        }

        @JavascriptInterface
        public void loadAccounts() {
            request(FinanceExtrasService.ACTION_LIST_ACCOUNTS, null, "GranaOkAccountsPlus");
        }

        @JavascriptInterface
        public void addAccount(String payloadJson) {
            request(FinanceExtrasService.ACTION_ADD_ACCOUNT, payloadJson, "GranaOkAccountPlusSaved");
        }
    }

    private void request(String action, String payloadJson, String callbackName) {
        try {
            String configJson = readSecureDbConfig().toString();
            ResultReceiver receiver = new ResultReceiver(new Handler(Looper.getMainLooper())) {
                @Override
                protected void onReceiveResult(int resultCode, Bundle resultData) {
                    String json = resultData == null ? "{}" : resultData.getString("json", "{}");
                    callback(callbackName, json);
                }
            };
            Intent intent = new Intent(this, FinanceExtrasService.class);
            intent.putExtra(FinanceExtrasService.EXTRA_ACTION, action);
            intent.putExtra(FinanceExtrasService.EXTRA_CONFIG, configJson);
            if (payloadJson != null) intent.putExtra(FinanceExtrasService.EXTRA_PAYLOAD, payloadJson);
            intent.putExtra(FinanceExtrasService.EXTRA_RECEIVER, receiver);
            startService(intent);
        } catch (Throwable e) {
            JSONObject out = new JSONObject();
            try { out.put("ok", false); out.put("error", cleanMessage(e)); } catch (Throwable ignored) {}
            callback(callbackName, out.toString());
        }
    }

    private JSONObject readSecureDbConfig() throws Exception {
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        String ivB64 = prefs.getString("db_iv", null);
        String cipherB64 = prefs.getString("db_cipher", null);
        if (ivB64 == null || cipherB64 == null) throw new IllegalStateException("Banco de dados ainda não configurado.");
        KeyStore keyStore = KeyStore.getInstance("AndroidKeyStore");
        keyStore.load(null);
        KeyStore.SecretKeyEntry entry = (KeyStore.SecretKeyEntry) keyStore.getEntry(KEY_ALIAS, null);
        if (entry == null) throw new IllegalStateException("Chave segura do banco não encontrada.");
        SecretKey key = entry.getSecretKey();
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(128, Base64.decode(ivB64, Base64.NO_WRAP)));
        byte[] plain = cipher.doFinal(Base64.decode(cipherB64, Base64.NO_WRAP));
        return new JSONObject(new String(plain, StandardCharsets.UTF_8));
    }

    private void callback(String fn, String json) {
        if (web == null || fn == null || fn.trim().isEmpty()) return;
        final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(json == null ? "{}" : json) + ")";
        runOnUiThread(() -> {
            if (web != null) {
                try { web.evaluateJavascript(js, null); } catch (Throwable ignored) {}
            }
        });
    }

    private static String cleanMessage(Throwable e) {
        String m = e == null ? "Falha desconhecida" : e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        m = m.replaceAll("(?i)password=[^&\\s]+", "password=***");
        return m.length() > 420 ? m.substring(0, 420) : m;
    }
}
