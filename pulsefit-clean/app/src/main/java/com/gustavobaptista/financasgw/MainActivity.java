package com.gustavobaptista.financasgw;

import android.Manifest;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.hardware.biometrics.BiometricPrompt;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.CancellationSignal;
import android.view.Gravity;
import android.view.View;
import android.webkit.ConsoleMessage;
import android.webkit.JavascriptInterface;
import android.webkit.RenderProcessGoneDetail;
import android.webkit.ValueCallback;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.Executor;

public final class MainActivity extends Activity {
    private static final int FILE_CHOOSER_REQUEST = 501;
    private static final int NOTIFICATION_PERMISSION_REQUEST = 502;
    private static final String BASE_URL = "https://financas.local/";

    private FrameLayout root;
    private WebView webView;
    private View loadingView;
    private ValueCallback<Uri[]> fileCallback;
    private boolean rebuilding;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setStatusBarColor(Color.rgb(8, 25, 35));
        getWindow().setNavigationBarColor(Color.rgb(8, 25, 35));
        root = new FrameLayout(this);
        root.setBackgroundColor(Color.rgb(8, 25, 35));
        setContentView(root);
        ReminderScheduler.rescheduleStored(this);
        buildWebView();
    }

    private void buildWebView() {
        try {
            showLoading("Carregando seu controle financeiro…");
            createWebView();
        } catch (Throwable error) {
            showNativeError("Não foi possível iniciar o aplicativo", error);
        }
    }

    @SuppressLint({"SetJavaScriptEnabled", "JavascriptInterface"})
    private void createWebView() throws Exception {
        destroyWebView();
        webView = new WebView(this);
        webView.setBackgroundColor(Color.rgb(8, 25, 35));
        webView.setOverScrollMode(View.OVER_SCROLL_NEVER);
        WebView.setWebContentsDebuggingEnabled(false);

        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setDefaultTextEncodingName("UTF-8");
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(false);
        settings.setAllowFileAccessFromFileURLs(false);
        settings.setAllowUniversalAccessFromFileURLs(false);
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        settings.setCacheMode(WebSettings.LOAD_DEFAULT);
        settings.setSupportMultipleWindows(false);
        settings.setBuiltInZoomControls(false);
        settings.setDisplayZoomControls(false);
        settings.setTextZoom(100);
        settings.setMediaPlaybackRequiresUserGesture(true);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) settings.setSafeBrowsingEnabled(true);

        webView.addJavascriptInterface(new NativeBridge(), "AndroidBridge");
        webView.setWebChromeClient(new WebChromeClient() {
            @Override
            public boolean onConsoleMessage(ConsoleMessage message) {
                android.util.Log.d("FinancasGW", message.message() + " @" + message.lineNumber());
                return true;
            }

            @Override
            public boolean onShowFileChooser(WebView view, ValueCallback<Uri[]> callback, FileChooserParams params) {
                if (fileCallback != null) fileCallback.onReceiveValue(null);
                fileCallback = callback;
                try {
                    Intent intent = params.createIntent();
                    intent.addCategory(Intent.CATEGORY_OPENABLE);
                    startActivityForResult(intent, FILE_CHOOSER_REQUEST);
                    return true;
                } catch (ActivityNotFoundException error) {
                    fileCallback = null;
                    Toast.makeText(MainActivity.this, "Nenhum seletor de arquivos disponível.", Toast.LENGTH_LONG).show();
                    return false;
                }
            }
        });
        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri uri = request.getUrl();
                if (BASE_URL.equals(uri.toString()) || "financas.local".equalsIgnoreCase(uri.getHost())) return false;
                openExternal(uri);
                return true;
            }

            @Override
            public void onPageFinished(WebView view, String url) {
                hideLoading();
            }

            @Override
            public void onReceivedError(WebView view, WebResourceRequest request, WebResourceError error) {
                if (request.isForMainFrame()) showNativeError("Não foi possível carregar a interface", new IllegalStateException(String.valueOf(error.getDescription())));
            }

            @Override
            public boolean onRenderProcessGone(WebView view, RenderProcessGoneDetail detail) {
                if (rebuilding) return true;
                rebuilding = true;
                destroyWebView();
                showLoading("Recuperando a interface…");
                root.postDelayed(() -> {
                    rebuilding = false;
                    buildWebView();
                    Toast.makeText(MainActivity.this, "A interface foi recuperada.", Toast.LENGTH_SHORT).show();
                }, 500L);
                return true;
            }
        });

        root.addView(webView, 0, new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT));
        webView.loadDataWithBaseURL(BASE_URL, readAsset("index.html"), "text/html", "UTF-8", null);
    }

    private String readAsset(String name) throws Exception {
        StringBuilder builder = new StringBuilder();
        try (InputStream in = getAssets().open(name); BufferedReader reader = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8))) {
            char[] buffer = new char[8192];
            int count;
            while ((count = reader.read(buffer)) >= 0) builder.append(buffer, 0, count);
        }
        return builder.toString();
    }

    private void openExternal(Uri uri) {
        if (uri == null || uri.getScheme() == null) return;
        String scheme = uri.getScheme();
        if (!scheme.equalsIgnoreCase("http") && !scheme.equalsIgnoreCase("https") && !scheme.equalsIgnoreCase("mailto") && !scheme.equalsIgnoreCase("tel")) return;
        try {
            startActivity(new Intent(Intent.ACTION_VIEW, uri));
        } catch (ActivityNotFoundException ignored) {
            Toast.makeText(this, "Não foi possível abrir esse link.", Toast.LENGTH_SHORT).show();
        }
    }

    private final class NativeBridge {
        @JavascriptInterface
        public String getCapabilities() {
            try {
                JSONObject object = new JSONObject();
                object.put("apk", true);
                object.put("biometric", biometricAvailable());
                object.put("notifications", true);
                object.put("version", "Android 1.0.0");
                return object.toString();
            } catch (Exception error) {
                return "{\"apk\":true,\"biometric\":false,\"notifications\":true,\"version\":\"Android 1.0.0\"}";
            }
        }

        @JavascriptInterface
        public void authenticateBiometric() {
            runOnUiThread(MainActivity.this::showBiometricPrompt);
        }

        @JavascriptInterface
        public void requestNotificationPermission() {
            runOnUiThread(MainActivity.this::requestNotifications);
        }

        @JavascriptInterface
        public void syncReminders(String json) {
            ReminderScheduler.sync(MainActivity.this, json);
        }

        @JavascriptInterface
        public void showTestNotification() {
            ReminderReceiver.showNotification(MainActivity.this, 990001, "Teste de vencimento", "As notificações do Finanças GW estão funcionando.");
            callbackNotification(true, "Notificação de teste enviada.");
        }
    }

    private boolean biometricAvailable() {
        return Build.VERSION.SDK_INT >= Build.VERSION_CODES.P && getPackageManager().hasSystemFeature(PackageManager.FEATURE_FINGERPRINT);
    }

    private void showBiometricPrompt() {
        if (!biometricAvailable()) {
            callbackBiometric(false, "Biometria indisponível neste aparelho.");
            return;
        }
        try {
            Executor executor = getMainExecutor();
            BiometricPrompt prompt = new BiometricPrompt.Builder(this)
                    .setTitle("Desbloquear Finanças GW")
                    .setSubtitle("Use sua digital ou biometria cadastrada")
                    .setDescription("A biometria confirma o acesso local ao aplicativo.")
                    .setNegativeButton("Cancelar", executor, (dialog, which) -> callbackBiometric(false, "Autenticação cancelada."))
                    .build();
            prompt.authenticate(new CancellationSignal(), executor, new BiometricPrompt.AuthenticationCallback() {
                @Override
                public void onAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) {
                    callbackBiometric(true, "Digital reconhecida.");
                }

                @Override
                public void onAuthenticationError(int errorCode, CharSequence errString) {
                    callbackBiometric(false, String.valueOf(errString));
                }

                @Override
                public void onAuthenticationFailed() {
                    callbackBiometric(false, "Digital não reconhecida.");
                }
            });
        } catch (Throwable error) {
            callbackBiometric(false, "Não foi possível abrir a biometria.");
        }
    }

    private void requestNotifications() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU && checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions(new String[]{Manifest.permission.POST_NOTIFICATIONS}, NOTIFICATION_PERMISSION_REQUEST);
        } else {
            ReminderReceiver.ensureChannel(this);
            callbackNotification(true, "Notificações autorizadas.");
        }
    }

    private void callbackBiometric(boolean success, String message) {
        runJs("window.onBiometricResult(" + success + "," + JSONObject.quote(message == null ? "" : message) + ")");
    }

    private void callbackNotification(boolean success, String message) {
        runJs("window.onNotificationResult(" + success + "," + JSONObject.quote(message == null ? "" : message) + ")");
    }

    private void runJs(String script) {
        runOnUiThread(() -> {
            if (webView != null) webView.evaluateJavascript(script, null);
        });
    }

    private void showLoading(String message) {
        if (root == null) return;
        if (loadingView != null) root.removeView(loadingView);
        LinearLayout panel = new LinearLayout(this);
        panel.setOrientation(LinearLayout.VERTICAL);
        panel.setGravity(Gravity.CENTER);
        panel.setPadding(48, 48, 48, 48);
        panel.setBackgroundColor(Color.rgb(8, 25, 35));
        ProgressBar progress = new ProgressBar(this);
        TextView text = new TextView(this);
        text.setText(message);
        text.setTextColor(Color.WHITE);
        text.setTextSize(15f);
        text.setPadding(0, 24, 0, 0);
        panel.addView(progress);
        panel.addView(text);
        loadingView = panel;
        root.addView(panel, new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT));
    }

    private void hideLoading() {
        if (loadingView != null && root != null) {
            root.removeView(loadingView);
            loadingView = null;
        }
    }

    private void showNativeError(String title, Throwable error) {
        destroyWebView();
        if (root == null) {
            root = new FrameLayout(this);
            setContentView(root);
        }
        root.removeAllViews();
        LinearLayout panel = new LinearLayout(this);
        panel.setOrientation(LinearLayout.VERTICAL);
        panel.setGravity(Gravity.CENTER_HORIZONTAL);
        panel.setPadding(48, 80, 48, 48);
        panel.setBackgroundColor(Color.rgb(8, 25, 35));
        TextView heading = new TextView(this);
        heading.setText(title);
        heading.setTextColor(Color.WHITE);
        heading.setTextSize(23f);
        TextView details = new TextView(this);
        details.setText("Seus dados permanecem salvos. Toque abaixo para tentar carregar novamente.\n\nDetalhe: " + error.getClass().getSimpleName());
        details.setTextColor(Color.rgb(154, 181, 193));
        details.setTextSize(14f);
        details.setPadding(0, 18, 0, 22);
        Button retry = new Button(this);
        retry.setText("Tentar novamente");
        retry.setOnClickListener(v -> {
            root.removeAllViews();
            buildWebView();
        });
        panel.addView(heading);
        panel.addView(details);
        panel.addView(retry);
        root.addView(panel, new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT));
    }

    private void destroyWebView() {
        if (webView == null) return;
        try {
            if (root != null) root.removeView(webView);
            webView.stopLoading();
            webView.loadUrl("about:blank");
            webView.removeJavascriptInterface("AndroidBridge");
            webView.setWebChromeClient(null);
            webView.setWebViewClient(null);
            webView.removeAllViews();
            webView.destroy();
        } catch (Throwable ignored) {
        } finally {
            webView = null;
        }
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == NOTIFICATION_PERMISSION_REQUEST) {
            boolean granted = grantResults.length > 0 && grantResults[0] == PackageManager.PERMISSION_GRANTED;
            if (granted) ReminderReceiver.ensureChannel(this);
            callbackNotification(granted, granted ? "Notificações autorizadas." : "Permissão de notificações negada.");
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == FILE_CHOOSER_REQUEST && fileCallback != null) {
            Uri[] result = WebChromeClient.FileChooserParams.parseResult(resultCode, data);
            fileCallback.onReceiveValue(result);
            fileCallback = null;
        }
    }

    @Override
    public void onBackPressed() {
        if (webView != null && webView.canGoBack()) webView.goBack();
        else super.onBackPressed();
    }

    @Override
    protected void onDestroy() {
        if (fileCallback != null) {
            fileCallback.onReceiveValue(null);
            fileCallback = null;
        }
        destroyWebView();
        super.onDestroy();
    }
}
