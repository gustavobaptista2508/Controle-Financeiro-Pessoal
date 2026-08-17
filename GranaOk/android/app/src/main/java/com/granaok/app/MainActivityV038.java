package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.ResultReceiver;
import android.util.Base64;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.JavascriptInterface;
import android.webkit.WebView;

import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.security.KeyStore;

import javax.crypto.Cipher;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/** Beta 0.3.8: navegação mensal + projeções/simulações locais. */
public class MainActivityV038 extends MainActivityV037 {
    private static final String PREFS = "granaok_secure";
    private static final String KEY_ALIAS = "granaok_db_key";
    private WebView web;

    @SuppressLint({"SetJavaScriptEnabled", "JavascriptInterface"})
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        web = findWebView();
        if (web != null) {
            web.addJavascriptInterface(new PlanningBridge(), "GranaPlanning");
        }
    }

    @Override
    protected void onDestroy() {
        if (web != null) {
            try { web.removeJavascriptInterface("GranaPlanning"); } catch (Throwable ignored) {}
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

    public class PlanningBridge {
        @JavascriptInterface
        public boolean isAvailable() { return true; }

        @JavascriptInterface
        public void loadMonth(String month) {
            JSONObject payload = new JSONObject();
            try { payload.put("month", month); } catch (Throwable ignored) {}
            request(PlanningService.ACTION_MONTH_SUMMARY, payload.toString(), "GranaOkMonthSummary");
        }

        @JavascriptInterface
        public void projectNextMonth(String baseMonth) {
            JSONObject payload = new JSONObject();
            try { payload.put("month", baseMonth); } catch (Throwable ignored) {}
            request(PlanningService.ACTION_NEXT_PROJECTION, payload.toString(), "GranaOkNextProjection");
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

            Intent intent = new Intent(this, PlanningService.class);
            intent.putExtra(PlanningService.EXTRA_ACTION, action);
            intent.putExtra(PlanningService.EXTRA_CONFIG, configJson);
            if (payloadJson != null) intent.putExtra(PlanningService.EXTRA_PAYLOAD, payloadJson);
            intent.putExtra(PlanningService.EXTRA_RECEIVER, receiver);
            startService(intent);
        } catch (Throwable e) {
            JSONObject out = new JSONObject();
            try {
                out.put("ok", false);
                out.put("error", cleanMessage(e));
            } catch (Throwable ignored) {}
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
        cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(128, Base64.decode(ivB64, Base64.NO_WRAP)));
        byte[] plain = cipher.doFinal(Base64.decode(cipherB64, Base64.NO_WRAP));
        return new JSONObject(new String(plain, StandardCharsets.UTF_8));
    }

    private void callback(String fn, String json) {
        if (web == null || fn == null || fn.trim().isEmpty()) return;
        final String safeJson = json == null ? "{}" : json;
        final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(safeJson) + ")";
        runOnUiThread(() -> {
            if (web != null) {
                try { web.evaluateJavascript(js, null); } catch (Throwable ignored) {}
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
