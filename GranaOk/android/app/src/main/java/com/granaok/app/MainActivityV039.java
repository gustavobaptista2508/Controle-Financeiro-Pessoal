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

/** Beta 0.3.9+: bridge de edição e filtros de lançamentos/contas. */
public class MainActivityV039 extends MainActivityV038 {
    private static final String PREFS = "granaok_secure";
    private static final String KEY_ALIAS = "granaok_db_key";
    private WebView manageWeb;

    @SuppressLint({"SetJavaScriptEnabled", "JavascriptInterface"})
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        manageWeb = findManageWebView();
        if (manageWeb != null) manageWeb.addJavascriptInterface(new ManageBridge(), "GranaManage");
    }

    @Override
    protected void onDestroy() {
        if (manageWeb != null) {
            try { manageWeb.removeJavascriptInterface("GranaManage"); } catch (Throwable ignored) {}
        }
        super.onDestroy();
    }

    private WebView findManageWebView() {
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

    public class ManageBridge {
        @JavascriptInterface public boolean isAvailable() { return true; }
        @JavascriptInterface public void loadTransactions(String filtersJson) {
            requestManage(FinanceManageService.ACTION_LIST_TRANSACTIONS, filtersJson, "GranaOkManagedTransactions");
        }
        @JavascriptInterface public void addTransaction(String payloadJson) {
            requestManage(FinanceManageService.ACTION_ADD_TRANSACTION, payloadJson, "GranaOkManagedTransactionSaved");
        }
        @JavascriptInterface public void updateTransaction(String payloadJson) {
            requestManage(FinanceManageService.ACTION_UPDATE_TRANSACTION, payloadJson, "GranaOkManagedTransactionUpdated");
        }
        @JavascriptInterface public void setTransactionStatus(long id, String status) {
            JSONObject p = new JSONObject();
            try { p.put("id", id); p.put("status", status); } catch (Throwable ignored) {}
            requestManage(FinanceManageService.ACTION_SET_STATUS, p.toString(), "GranaOkManagedStatusUpdated");
        }
        @JavascriptInterface public void updateAccount(String payloadJson) {
            requestManage(FinanceManageService.ACTION_UPDATE_ACCOUNT, payloadJson, "GranaOkManagedAccountUpdated");
        }
    }

    private void requestManage(String action, String payloadJson, String callbackName) {
        try {
            String configJson = readManageDbConfig().toString();
            ResultReceiver receiver = new ResultReceiver(new Handler(Looper.getMainLooper())) {
                @Override protected void onReceiveResult(int resultCode, Bundle resultData) {
                    String json = resultData == null ? "{}" : resultData.getString("json", "{}");
                    callbackManage(callbackName, json);
                }
            };
            Intent intent = new Intent(this, FinanceManageService.class);
            intent.putExtra(FinanceManageService.EXTRA_ACTION, action);
            intent.putExtra(FinanceManageService.EXTRA_CONFIG, configJson);
            if (payloadJson != null) intent.putExtra(FinanceManageService.EXTRA_PAYLOAD, payloadJson);
            intent.putExtra(FinanceManageService.EXTRA_RECEIVER, receiver);
            startService(intent);
        } catch (Throwable e) {
            JSONObject out = new JSONObject();
            try { out.put("ok", false); out.put("error", cleanManageMessage(e)); } catch (Throwable ignored) {}
            callbackManage(callbackName, out.toString());
        }
    }

    private JSONObject readManageDbConfig() throws Exception {
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        String ivB64 = prefs.getString("db_iv", null);
        String cipherB64 = prefs.getString("db_cipher", null);
        if (ivB64 == null || cipherB64 == null) throw new IllegalStateException("Banco de dados ainda não configurado.");
        KeyStore keyStore = KeyStore.getInstance("AndroidKeyStore"); keyStore.load(null);
        KeyStore.SecretKeyEntry entry = (KeyStore.SecretKeyEntry) keyStore.getEntry(KEY_ALIAS, null);
        if (entry == null) throw new IllegalStateException("Chave segura do banco não encontrada.");
        SecretKey key = entry.getSecretKey();
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(128, Base64.decode(ivB64, Base64.NO_WRAP)));
        byte[] plain = cipher.doFinal(Base64.decode(cipherB64, Base64.NO_WRAP));
        return new JSONObject(new String(plain, StandardCharsets.UTF_8));
    }

    private void callbackManage(String fn, String json) {
        if (manageWeb == null || fn == null || fn.trim().isEmpty()) return;
        final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(json == null ? "{}" : json) + ")";
        runOnUiThread(() -> { if (manageWeb != null) try { manageWeb.evaluateJavascript(js, null); } catch (Throwable ignored) {} });
    }

    private static String cleanManageMessage(Throwable e) {
        String m = e == null ? "Falha desconhecida" : e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        m = m.replaceAll("(?i)password=[^&\\s]+", "password=***");
        return m.length() > 420 ? m.substring(0, 420) : m;
    }
}
