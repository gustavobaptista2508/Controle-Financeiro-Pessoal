package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.graphics.Insets;
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

/**
 * Camada de UI da Beta 0.3.6.
 * Mantém a barra de status visível no Android 15/16 e adiciona um bridge
 * separado para os cadastros financeiros, sem carregar JDBC no processo da UI.
 */
public class MainActivityUi extends MainActivitySafe {
    private static final String PREFS = "granaok_secure";
    private static final String KEY_ALIAS = "granaok_db_key";

    private WebView financeWebView;

    @SuppressLint({"SetJavaScriptEnabled", "JavascriptInterface"})
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        financeWebView = findWebView();
        configureSystemBars();
        if (financeWebView != null) {
            financeWebView.addJavascriptInterface(new FinanceBridge(), "GranaFinance");
        }
    }

    @Override
    protected void onDestroy() {
        if (financeWebView != null) {
            try {
                financeWebView.removeJavascriptInterface("GranaFinance");
            } catch (Throwable ignored) {
            }
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
        } catch (Throwable ignored) {
        }
        return null;
    }

    private void configureSystemBars() {
        Window window = getWindow();
        window.clearFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN);
        window.addFlags(WindowManager.LayoutParams.FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);
        window.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE);

        if (Build.VERSION.SDK_INT < 35) {
            window.setStatusBarColor(Color.parseColor("#F7F8FA"));
            window.setNavigationBarColor(Color.WHITE);
        } else {
            // Android 15+ força edge-to-edge para apps target 35. Mantemos as barras
            // transparentes e aplicamos os insets no WebView para o conteúdo não ficar por baixo.
            window.setStatusBarColor(Color.TRANSPARENT);
            window.setNavigationBarColor(Color.TRANSPARENT);
        }

        int legacyFlags = 0;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            legacyFlags |= View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR;
        }
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            legacyFlags |= View.SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR;
        }
        window.getDecorView().setSystemUiVisibility(legacyFlags);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            WindowInsetsController controller = window.getInsetsController();
            if (controller != null) {
                controller.show(WindowInsets.Type.statusBars() | WindowInsets.Type.navigationBars());
                controller.setSystemBarsAppearance(
                    WindowInsetsController.APPEARANCE_LIGHT_STATUS_BARS |
                        WindowInsetsController.APPEARANCE_LIGHT_NAVIGATION_BARS,
                    WindowInsetsController.APPEARANCE_LIGHT_STATUS_BARS |
                        WindowInsetsController.APPEARANCE_LIGHT_NAVIGATION_BARS
                );
            }
        }

        if (Build.VERSION.SDK_INT >= 35 && financeWebView != null) {
            financeWebView.setOnApplyWindowInsetsListener((view, insets) -> {
                Insets bars = insets.getInsets(
                    WindowInsets.Type.statusBars() |
                        WindowInsets.Type.navigationBars() |
                        WindowInsets.Type.displayCutout()
                );
                view.setPadding(0, bars.top, 0, bars.bottom);
                return insets;
            });
            financeWebView.requestApplyInsets();
        }
    }

    public class FinanceBridge {
        @JavascriptInterface
        public boolean isAvailable() {
            return true;
        }

        @JavascriptInterface
        public void loadAccounts() {
            request(FinanceWorkerService.ACTION_LIST_ACCOUNTS, null, "GranaOkAccounts");
        }

        @JavascriptInterface
        public void addAccount(String payloadJson) {
            request(FinanceWorkerService.ACTION_ADD_ACCOUNT, payloadJson, "GranaOkAccountSaved");
        }

        @JavascriptInterface
        public void loadCards() {
            request(FinanceWorkerService.ACTION_LIST_CARDS, null, "GranaOkCards");
        }

        @JavascriptInterface
        public void addCard(String payloadJson) {
            request(FinanceWorkerService.ACTION_ADD_CARD, payloadJson, "GranaOkCardSaved");
        }

        @JavascriptInterface
        public void loadFinancings() {
            request(FinanceWorkerService.ACTION_LIST_FINANCINGS, null, "GranaOkFinancings");
        }

        @JavascriptInterface
        public void addFinancing(String payloadJson) {
            request(FinanceWorkerService.ACTION_ADD_FINANCING, payloadJson, "GranaOkFinancingSaved");
        }

        @JavascriptInterface
        public void loadOverview() {
            request(FinanceWorkerService.ACTION_OVERVIEW, null, "GranaOkFinanceOverview");
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

            Intent intent = new Intent(this, FinanceWorkerService.class);
            intent.putExtra(FinanceWorkerService.EXTRA_ACTION, action);
            intent.putExtra(FinanceWorkerService.EXTRA_CONFIG, configJson);
            if (payloadJson != null) intent.putExtra(FinanceWorkerService.EXTRA_PAYLOAD, payloadJson);
            intent.putExtra(FinanceWorkerService.EXTRA_RECEIVER, receiver);
            startService(intent);
        } catch (Throwable e) {
            JSONObject out = new JSONObject();
            try {
                out.put("ok", false);
                out.put("error", cleanMessage(e));
            } catch (Throwable ignored) {
            }
            callback(callbackName, out.toString());
        }
    }

    private JSONObject readSecureDbConfig() throws Exception {
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        String ivB64 = prefs.getString("db_iv", null);
        String cipherB64 = prefs.getString("db_cipher", null);
        if (ivB64 == null || cipherB64 == null) {
            throw new IllegalStateException("Banco de dados ainda não configurado.");
        }

        KeyStore keyStore = KeyStore.getInstance("AndroidKeyStore");
        keyStore.load(null);
        KeyStore.SecretKeyEntry entry = (KeyStore.SecretKeyEntry) keyStore.getEntry(KEY_ALIAS, null);
        if (entry == null) throw new IllegalStateException("Chave segura do banco não encontrada.");
        SecretKey key = entry.getSecretKey();

        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(
            Cipher.DECRYPT_MODE,
            key,
            new GCMParameterSpec(128, Base64.decode(ivB64, Base64.NO_WRAP))
        );
        byte[] plain = cipher.doFinal(Base64.decode(cipherB64, Base64.NO_WRAP));
        return new JSONObject(new String(plain, StandardCharsets.UTF_8));
    }

    private void callback(String fn, String json) {
        if (financeWebView == null || fn == null || fn.trim().isEmpty()) return;
        final String safeJson = json == null ? "{}" : json;
        final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(safeJson) + ")";
        runOnUiThread(() -> {
            if (financeWebView != null) {
                try {
                    financeWebView.evaluateJavascript(js, null);
                } catch (Throwable ignored) {
                }
            }
        });
    }

    private static String cleanMessage(Throwable e) {
        String message = e == null ? "Falha desconhecida" : e.getMessage();
        if (message == null || message.trim().isEmpty()) {
            message = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        }
        message = message.replaceAll("(?i)password=[^&\\s]+", "password=***");
        message = message.replaceAll("(?i)(password|senha)[=:]\\s*[^\\s,;]+", "$1=***");
        return message.length() > 420 ? message.substring(0, 420) : message;
    }
}
