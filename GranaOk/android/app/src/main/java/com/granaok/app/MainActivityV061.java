package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.util.Base64;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.JavascriptInterface;
import android.webkit.WebView;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.security.SecureRandom;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/**
 * Beta 0.6.1: conecta o APK ao mesmo Motor de Conhecimento Financeiro da VPS.
 * Nenhuma credencial de banco ou senha do GranaOk é embutida no APK.
 * A sessão da API é curta e protegida pelo Android Keystore.
 */
public class MainActivityV061 extends MainActivityV060 {
    private static final String API_BASE = "https://granaok.com.br";
    private static final String PREFS = "granaok_server_ai";
    private static final String KEY_ALIAS = "granaok_server_session_key";
    private static final Set<String> ALLOWED = new HashSet<>(Arrays.asList(
        "assistant_ask","assistant_summary","knowledge_summary","knowledge_rebuild","investment_radar"
    ));
    private WebView web061;

    @SuppressLint({"SetJavaScriptEnabled","JavascriptInterface"})
    @Override protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        web061 = findWebViewRecursive(findViewById(android.R.id.content));
        if (web061 != null) web061.addJavascriptInterface(new ServerAiBridge(), "GranaServerAI");
    }

    @Override protected void onDestroy() {
        if (web061 != null) try { web061.removeJavascriptInterface("GranaServerAI"); } catch (Throwable ignored) {}
        super.onDestroy();
    }

    private WebView findWebViewRecursive(View view) {
        if (view instanceof WebView) return (WebView) view;
        if (view instanceof ViewGroup) {
            ViewGroup g=(ViewGroup)view;
            for(int i=0;i<g.getChildCount();i++){ WebView w=findWebViewRecursive(g.getChildAt(i)); if(w!=null)return w; }
        }
        return null;
    }

    public class ServerAiBridge {
        @JavascriptInterface public boolean isAvailable(){ return true; }
        @JavascriptInterface public boolean hasSession(){ return readSession()!=null; }
        @JavascriptInterface public void logout(){ clearSession(); callback("GranaOkServerAiLogin", json(false,"Sessão removida.")); }

        @JavascriptInterface public void login(String username,String password){
            final String u=username==null?"":username.trim(), p=password==null?"":password;
            new Thread(() -> {
                try {
                    JSONObject body=new JSONObject(); body.put("username",u); body.put("password",p);
                    HttpResult r=post("/api/login",body.toString(),null,false);
                    JSONObject out=new JSONObject(r.body);
                    if(r.code<200||r.code>=300||!out.optBoolean("ok",false))
                        throw new IllegalStateException(out.optString("error","Usuário ou senha inválidos."));
                    String token=sessionFromSetCookie(r.setCookie);
                    if(token==null||token.isEmpty())throw new IllegalStateException("Servidor não retornou sessão.");
                    saveSession(token);
                    callback("GranaOkServerAiLogin",out.toString());
                } catch(Throwable e){ callback("GranaOkServerAiLogin",json(false,clean(e))); }
            },"GranaOk-ServerAI-Login").start();
        }

        @JavascriptInterface public void action(String action,String payloadJson){
            final String a=action==null?"":action.trim();
            if(!ALLOWED.contains(a)){ callback("GranaOkServerAiResult",json(false,"Operação não permitida.")); return; }
            new Thread(() -> {
                try{
                    String session=readSession();
                    if(session==null)throw new IllegalStateException("Conecte a Grana IA ao servidor primeiro.");
                    JSONObject body=new JSONObject();
                    body.put("action",a);
                    body.put("payload",payloadJson==null||payloadJson.trim().isEmpty()?new JSONObject():new JSONObject(payloadJson));
                    HttpResult r=post("/api/action",body.toString(),session,true);
                    JSONObject out=new JSONObject(r.body);
                    if(r.code==401){ clearSession(); throw new IllegalStateException("Sessão da Grana IA expirou. Conecte novamente."); }
                    if(r.code<200||r.code>=300||!out.optBoolean("ok",false))
                        throw new IllegalStateException(out.optString("error","Falha na Grana IA."));
                    callback("GranaOkServerAiResult",out.toString());
                }catch(Throwable e){ callback("GranaOkServerAiResult",json(false,clean(e))); }
            },"GranaOk-ServerAI-Action").start();
        }
    }

    private HttpResult post(String path,String json,String session,boolean webClient) throws Exception{
        HttpURLConnection c=(HttpURLConnection)new URL(API_BASE+path).openConnection();
        c.setConnectTimeout(9000); c.setReadTimeout(18000); c.setRequestMethod("POST");
        c.setDoOutput(true); c.setInstanceFollowRedirects(false);
        c.setRequestProperty("Content-Type","application/json; charset=utf-8");
        c.setRequestProperty("Accept","application/json");
        c.setRequestProperty("User-Agent","GranaOk-Android/0.6.1");
        if(webClient)c.setRequestProperty("X-GranaOk-Client","web");
        if(session!=null)c.setRequestProperty("Cookie","gk_session="+session);
        byte[] data=json.getBytes(StandardCharsets.UTF_8);
        c.setFixedLengthStreamingMode(data.length);
        try(OutputStream out=c.getOutputStream()){ out.write(data); }
        int code=c.getResponseCode();
        String setCookie=c.getHeaderField("Set-Cookie");
        InputStream in=code>=200&&code<400?c.getInputStream():c.getErrorStream();
        String body=read(in); c.disconnect();
        return new HttpResult(code,body,setCookie);
    }

    private static String read(InputStream in)throws Exception{
        if(in==null)return "{}";
        BufferedReader r=new BufferedReader(new InputStreamReader(in,StandardCharsets.UTF_8));
        StringBuilder b=new StringBuilder();String line;
        while((line=r.readLine())!=null&&b.length()<500000)b.append(line);
        r.close();return b.toString();
    }

    private String sessionFromSetCookie(String header){
        if(header==null)return null;
        for(String p:header.split(";")){
            String t=p.trim(); if(t.startsWith("gk_session="))return t.substring("gk_session=".length());
        }
        return null;
    }

    private void saveSession(String value)throws Exception{
        SecretKey key=getOrCreateKey();
        byte[] iv=new byte[12];new SecureRandom().nextBytes(iv);
        Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.ENCRYPT_MODE,key,new GCMParameterSpec(128,iv));
        byte[] enc=cipher.doFinal(value.getBytes(StandardCharsets.UTF_8));
        getSharedPreferences(PREFS,MODE_PRIVATE).edit()
            .putString("iv",Base64.encodeToString(iv,Base64.NO_WRAP))
            .putString("cipher",Base64.encodeToString(enc,Base64.NO_WRAP)).apply();
    }

    private String readSession(){
        try{
            SharedPreferences p=getSharedPreferences(PREFS,MODE_PRIVATE);
            String iv=p.getString("iv",null), enc=p.getString("cipher",null);
            if(iv==null||enc==null)return null;
            KeyStore ks=KeyStore.getInstance("AndroidKeyStore");ks.load(null);
            KeyStore.SecretKeyEntry e=(KeyStore.SecretKeyEntry)ks.getEntry(KEY_ALIAS,null);
            if(e==null)return null;
            Cipher c=Cipher.getInstance("AES/GCM/NoPadding");
            c.init(Cipher.DECRYPT_MODE,e.getSecretKey(),new GCMParameterSpec(128,Base64.decode(iv,Base64.NO_WRAP)));
            return new String(c.doFinal(Base64.decode(enc,Base64.NO_WRAP)),StandardCharsets.UTF_8);
        }catch(Throwable e){clearSession();return null;}
    }

    private SecretKey getOrCreateKey()throws Exception{
        KeyStore ks=KeyStore.getInstance("AndroidKeyStore");ks.load(null);
        if(ks.containsAlias(KEY_ALIAS)){
            KeyStore.SecretKeyEntry e=(KeyStore.SecretKeyEntry)ks.getEntry(KEY_ALIAS,null);return e.getSecretKey();
        }
        KeyGenerator kg=KeyGenerator.getInstance("AES","AndroidKeyStore");
        android.security.keystore.KeyGenParameterSpec spec=new android.security.keystore.KeyGenParameterSpec.Builder(
            KEY_ALIAS,
            android.security.keystore.KeyProperties.PURPOSE_ENCRYPT|android.security.keystore.KeyProperties.PURPOSE_DECRYPT
        ).setBlockModes(android.security.keystore.KeyProperties.BLOCK_MODE_GCM)
         .setEncryptionPaddings(android.security.keystore.KeyProperties.ENCRYPTION_PADDING_NONE)
         .setKeySize(256).build();
        kg.init(spec);return kg.generateKey();
    }

    private void clearSession(){getSharedPreferences(PREFS,MODE_PRIVATE).edit().clear().apply();}

    private void callback(String fn,String json){
        if(web061==null)return;
        final String js="window."+fn+" && window."+fn+"("+JSONObject.quote(json==null?"{}":json)+")";
        runOnUiThread(() -> { if(web061!=null)try{web061.evaluateJavascript(js,null);}catch(Throwable ignored){} });
    }

    private static String json(boolean ok,String message){
        JSONObject o=new JSONObject();try{o.put("ok",ok);if(message!=null)o.put(ok?"message":"error",message);}catch(Throwable ignored){}return o.toString();
    }
    private static String clean(Throwable e){
        String m=e==null?"Falha desconhecida":e.getMessage();
        if(m==null||m.trim().isEmpty())m=e==null?"Falha desconhecida":e.getClass().getSimpleName();
        return m.replaceAll("(?i)(password|senha)=[^&\\s]+","$1=***");
    }
    private static class HttpResult{
        final int code;final String body;final String setCookie;
        HttpResult(int code,String body,String setCookie){this.code=code;this.body=body==null?"{}":body;this.setCookie=setCookie;}
    }
}
