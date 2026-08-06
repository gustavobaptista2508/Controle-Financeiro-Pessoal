package com.gustavobaptista.pulsefitclean;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.graphics.Color;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.view.View;
import android.webkit.ConsoleMessage;
import android.webkit.CookieManager;
import android.webkit.RenderProcessGoneDetail;
import android.webkit.ValueCallback;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.webkit.WebViewAssetLoader;
import androidx.webkit.WebViewClientCompat;

public final class MainActivity extends Activity {
    private static final String APP_URL = "https://appassets.androidplatform.net/assets/index.html";
    private static final int FILE_CHOOSER_REQUEST = 4102;

    private final Handler mainHandler = new Handler(Looper.getMainLooper());
    private FrameLayout root;
    private WebView webView;
    private View loadingView;
    private ValueCallback<Uri[]> fileCallback;
    private Bundle pendingWebState;
    private boolean rebuildingWebView;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        configureSystemBars();
        root = new FrameLayout(this);
        root.setBackgroundColor(Color.rgb(7, 25, 35));
        setContentView(root);
        pendingWebState = savedInstanceState;
        buildWebViewSafely();
    }

    private void configureSystemBars() {
        getWindow().setStatusBarColor(Color.rgb(7, 25, 35));
        getWindow().setNavigationBarColor(Color.rgb(7, 25, 35));
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            getWindow().getDecorView().setSystemUiVisibility(0);
        }
    }

    private void buildWebViewSafely() {
        try {
            createLoadingView(getString(com.gustavobaptista.pulsefitclean.R.string.loading));
            createWebView();
        } catch (Throwable error) {
            showNativeError("Não foi possível iniciar o PulseFit", error);
        }
    }

    @SuppressLint("SetJavaScriptEnabled")
    private void createWebView() {
        removeCurrentWebView();

        final WebViewAssetLoader assetLoader = new WebViewAssetLoader.Builder()
                .addPathHandler("/assets/", new WebViewAssetLoader.AssetsPathHandler(this))
                .build();

        webView = new WebView(this);
        webView.setBackgroundColor(Color.rgb(7, 25, 35));
        webView.setOverScrollMode(View.OVER_SCROLL_NEVER);
        webView.setHorizontalScrollBarEnabled(false);
        webView.setVerticalScrollBarEnabled(false);
        WebView.setWebContentsDebuggingEnabled(BuildConfig.DEBUG);

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
        settings.setMediaPlaybackRequiresUserGesture(true);
        settings.setSupportMultipleWindows(false);
        settings.setBuiltInZoomControls(false);
        settings.setDisplayZoomControls(false);
        settings.setTextZoom(100);
        settings.setSaveFormData(false);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            settings.setSafeBrowsingEnabled(true);
        }

        CookieManager.getInstance().setAcceptCookie(false);
        CookieManager.getInstance().setAcceptThirdPartyCookies(webView, false);

        webView.setWebViewClient(new SafeWebViewClient(assetLoader));
        webView.setWebChromeClient(new SafeChromeClient());
        webView.setDownloadListener((url, userAgent, contentDisposition, mimetype, contentLength) -> {
            if (url != null && (url.startsWith("http://") || url.startsWith("https://"))) {
                openExternal(Uri.parse(url));
            }
        });

        root.addView(webView, 0, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
        ));

        if (pendingWebState != null && webView.restoreState(pendingWebState) != null) {
            pendingWebState = null;
        } else {
            pendingWebState = null;
            webView.loadUrl(APP_URL);
        }
    }

    private final class SafeWebViewClient extends WebViewClientCompat {
        private final WebViewAssetLoader assetLoader;

        SafeWebViewClient(WebViewAssetLoader assetLoader) {
            this.assetLoader = assetLoader;
        }

        @Nullable
        @Override
        public WebResourceResponse shouldInterceptRequest(@NonNull WebView view, @NonNull WebResourceRequest request) {
            return assetLoader.shouldInterceptRequest(request.getUrl());
        }

        @Override
        public boolean shouldOverrideUrlLoading(@NonNull WebView view, @NonNull WebResourceRequest request) {
            Uri uri = request.getUrl();
            if ("appassets.androidplatform.net".equalsIgnoreCase(uri.getHost())) {
                return false;
            }
            openExternal(uri);
            return true;
        }

        @Override
        public void onPageFinished(@NonNull WebView view, @NonNull String url) {
            hideLoadingView();
        }

        @Override
        public void onReceivedError(@NonNull WebView view, @NonNull WebResourceRequest request, @NonNull WebResourceError error) {
            if (request.isForMainFrame()) {
                showNativeError("Não foi possível carregar a interface", new IllegalStateException(String.valueOf(error.getDescription())));
            }
        }

        @Override
        public boolean onRenderProcessGone(WebView view, RenderProcessGoneDetail detail) {
            if (rebuildingWebView) {
                return true;
            }
            rebuildingWebView = true;
            removeCurrentWebView();
            createLoadingView(detail.didCrash() ? "Recuperando o PulseFit…" : "Reiniciando a interface…");
            mainHandler.postDelayed(() -> {
                rebuildingWebView = false;
                buildWebViewSafely();
                Toast.makeText(MainActivity.this, "A interface foi recuperada.", Toast.LENGTH_SHORT).show();
            }, 400L);
            return true;
        }
    }

    private final class SafeChromeClient extends WebChromeClient {
        @Override
        public boolean onConsoleMessage(ConsoleMessage message) {
            android.util.Log.d("PulseFitWeb", message.message() + " @" + message.lineNumber());
            return true;
        }

        @Override
        public boolean onShowFileChooser(WebView view, ValueCallback<Uri[]> callback, FileChooserParams params) {
            if (fileCallback != null) {
                fileCallback.onReceiveValue(null);
            }
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
    }

    private void openExternal(Uri uri) {
        if (uri == null) {
            return;
        }
        String scheme = uri.getScheme();
        if (scheme == null) {
            return;
        }
        if (!scheme.equalsIgnoreCase("http") && !scheme.equalsIgnoreCase("https") && !scheme.equalsIgnoreCase("mailto") && !scheme.equalsIgnoreCase("tel")) {
            return;
        }
        try {
            startActivity(new Intent(Intent.ACTION_VIEW, uri));
        } catch (ActivityNotFoundException ignored) {
            Toast.makeText(this, "Não foi possível abrir esse link.", Toast.LENGTH_SHORT).show();
        }
    }

    private void createLoadingView(String message) {
        if (loadingView != null) {
            root.removeView(loadingView);
        }
        LinearLayout panel = new LinearLayout(this);
        panel.setOrientation(LinearLayout.VERTICAL);
        panel.setGravity(Gravity.CENTER);
        panel.setPadding(48, 48, 48, 48);
        panel.setBackgroundColor(Color.rgb(7, 25, 35));

        ProgressBar progress = new ProgressBar(this);
        TextView text = new TextView(this);
        text.setText(message);
        text.setTextColor(Color.rgb(238, 247, 251));
        text.setTextSize(15f);
        text.setGravity(Gravity.CENTER);
        text.setPadding(0, 24, 0, 0);

        panel.addView(progress);
        panel.addView(text);
        loadingView = panel;
        root.addView(panel, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
        ));
    }

    private void hideLoadingView() {
        if (loadingView != null && root != null) {
            root.removeView(loadingView);
            loadingView = null;
        }
    }

    private void showNativeError(String title, Throwable error) {
        removeCurrentWebView();
        if (root == null) {
            root = new FrameLayout(this);
            setContentView(root);
        }
        root.removeAllViews();

        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        layout.setGravity(Gravity.CENTER_HORIZONTAL);
        layout.setPadding(48, 80, 48, 48);
        layout.setBackgroundColor(Color.rgb(7, 25, 35));

        TextView heading = new TextView(this);
        heading.setText(title);
        heading.setTextColor(Color.WHITE);
        heading.setTextSize(23f);
        heading.setPadding(0, 0, 0, 18);

        TextView details = new TextView(this);
        details.setText("Feche e abra o aplicativo ou toque em tentar novamente. Seus dados permanecem no armazenamento do app.\n\nDetalhe: " + error.getClass().getSimpleName());
        details.setTextColor(Color.rgb(159, 183, 195));
        details.setTextSize(14f);
        details.setPadding(0, 0, 0, 24);

        Button retry = new Button(this);
        retry.setText(com.gustavobaptista.pulsefitclean.R.string.retry);
        retry.setOnClickListener(v -> {
            root.removeAllViews();
            buildWebViewSafely();
        });

        layout.addView(heading);
        layout.addView(details);
        layout.addView(retry);
        root.addView(layout, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
        ));
    }

    private void removeCurrentWebView() {
        if (webView == null) {
            return;
        }
        try {
            if (root != null) {
                root.removeView(webView);
            }
            webView.stopLoading();
            webView.loadUrl("about:blank");
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
    protected void onSaveInstanceState(@NonNull Bundle outState) {
        if (webView != null) {
            webView.saveState(outState);
        }
        super.onSaveInstanceState(outState);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == FILE_CHOOSER_REQUEST && fileCallback != null) {
            Uri[] result = WebChromeClient.FileChooserParams.parseResult(resultCode, data);
            fileCallback.onReceiveValue(result);
            fileCallback = null;
        }
    }

    @Override
    public void onBackPressed() {
        if (webView != null && webView.canGoBack()) {
            webView.goBack();
        } else {
            super.onBackPressed();
        }
    }

    @Override
    protected void onDestroy() {
        mainHandler.removeCallbacksAndMessages(null);
        if (fileCallback != null) {
            fileCallback.onReceiveValue(null);
            fileCallback = null;
        }
        removeCurrentWebView();
        super.onDestroy();
    }
}
