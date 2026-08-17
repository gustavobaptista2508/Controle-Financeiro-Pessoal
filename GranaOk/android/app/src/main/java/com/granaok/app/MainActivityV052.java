package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.ResultReceiver;
import android.security.keystore.KeyProperties;
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

/** Beta 0.5.2: controle de vencimento e pagamento de parcelas de financiamentos. */
public class MainActivityV052 extends MainActivityV051 {
    private static final String PREFS = "granaok_secure";
    private static final String KEY_ALIAS = "granaok_db_key";
    private WebView web052;

    @SuppressLint({"SetJavaScriptEnabled", "JavascriptInterface"})
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        web052 = findWebViewRecursive(findViewById(android.R.id.content));
        if (web052 != null) web052.addJavascriptInterface(new FinancingBridge(), "GranaFinancing");
    }

    @Override
    protected void onDestroy() {
        if (web052 != null) try { web052.removeJavascriptInterface("GranaFinancing"); } catch (Throwable ignored) {}
        super.onDestroy();
    }

    private WebView findWebViewRecursive(View view) {
        if (view instanceof WebView) return (WebView) view;
        if (view instanceof ViewGroup) {
            ViewGroup g = (ViewGroup) view;
            for (int i = 0; i < g.getChildCount(); i++) {
                WebView w = findWebViewRecursive(g.getChildAt(i));
                if (w != null) return w;
            }
        }
        return null;
    }

    public class FinancingBridge {
        @JavascriptInterface public boolean isAvailable() { return true; }
        @JavascriptInterface public void loadFinancings() { request(FinancingLifecycleService.ACTION_LIST, null, "GranaOkFinancings052"); }
        @JavascriptInterface public void addFinancing(String payloadJson) { request(FinancingLifecycleService.ACTION_ADD, payloadJson, "GranaOkFinancingSaved052"); }
        @JavascriptInterface public void setNextDue(String payloadJson) { request(FinancingLifecycleService.ACTION_SET_NEXT_DUE, payloadJson, "GranaOkFinancingScheduleSaved052"); }
        @JavascriptInterface public void payCurrent(String payloadJson) { request(FinancingLifecycleService.ACTION_PAY_CURRENT, payloadJson, "GranaOkFinancingPaid052"); }
        @JavascriptInterface public void loadInstallments(long financingId) {
            JSONObject p = new JSONObject(); try { p.put("financingId", financingId); } catch (Throwable ignored) {}
            request(FinancingLifecycleService.ACTION_INSTALLMENTS, p.toString(), "GranaOkFinancingInstallments052");
        }
    }

    private void request(String action, String payloadJson, String callbackName) {
        try {
            ResultReceiver receiver = new ResultReceiver(new Handler(Looper.getMainLooper())) {
                @Override protected void onReceiveResult(int resultCode, Bundle resultData) {
                    callback(callbackName, resultData == null ? "{}" : resultData.getString("json", "{}"));
                }
            };
            Intent i = new Intent(this, FinancingLifecycleService.class);
            i.putExtra(FinancingLifecycleService.EXTRA_ACTION, action);
            i.putExtra(FinancingLifecycleService.EXTRA_CONFIG, readSecureConfig().toString());
            if (payloadJson != null) i.putExtra(FinancingLifecycleService.EXTRA_PAYLOAD, payloadJson);
            i.putExtra(FinancingLifecycleService.EXTRA_RECEIVER, receiver);
            startService(i);
        } catch (Throwable e) {
            JSONObject out = new JSONObject();
            try { out.put("ok", false); out.put("error", clean(e)); } catch (Throwable ignored) {}
            callback(callbackName, out.toString());
        }
    }

    private JSONObject readSecureConfig() throws Exception {
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        String ivB64 = prefs.getString("db_iv", null), cipherB64 = prefs.getString("db_cipher", null);
        if (ivB64 == null || cipherB64 == null) throw new IllegalStateException("Banco de dados ainda não configurado.");
        KeyStore ks = KeyStore.getInstance("AndroidKeyStore"); ks.load(null);
        KeyStore.SecretKeyEntry entry = (KeyStore.SecretKeyEntry) ks.getEntry(KEY_ALIAS, null);
        if (entry == null) throw new IllegalStateException("Chave segura do banco não encontrada.");
        SecretKey key = entry.getSecretKey();
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(128, Base64.decode(ivB64, Base64.NO_WRAP)));
        return new JSONObject(new String(cipher.doFinal(Base64.decode(cipherB64, Base64.NO_WRAP)), StandardCharsets.UTF_8));
    }

    private void callback(String fn, String json) {
        if (web052 == null || fn == null || fn.trim().isEmpty()) return;
        final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(json == null ? "{}" : json) + ")";
        runOnUiThread(() -> { if (web052 != null) try { web052.evaluateJavascript(js, null); } catch (Throwable ignored) {} });
    }

    private static String clean(Throwable e) {
        String m = e == null ? "Falha desconhecida" : e.getMessage();
        if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName();
        m = m.replaceAll("(?i)password=[^&\\s]+", "password=***");
        return m.length() > 420 ? m.substring(0, 420) : m;
    }
}
