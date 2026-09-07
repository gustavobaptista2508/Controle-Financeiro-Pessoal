package br.com.granaok.app.update

import android.app.AlertDialog
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.provider.Settings
import android.widget.Toast
import androidx.core.content.FileProvider
import androidx.fragment.app.FragmentActivity
import androidx.lifecycle.lifecycleScope
import br.com.granaok.app.BuildConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.File
import java.io.FileOutputStream
import java.net.HttpURLConnection
import java.net.URI
import java.net.URL
import java.security.MessageDigest

class AppUpdateManager(
    private val activity: FragmentActivity,
) {
    data class UpdateInfo(
        val versionCode: Int,
        val versionName: String,
        val apkUrl: String,
        val sha256: String,
        val notes: String,
    )

    private var pendingInstall: UpdateInfo? = null
    private var dialogVisible = false

    fun checkAtStartup() {
        activity.lifecycleScope.launch {
            val info = runCatching { withContext(Dispatchers.IO) { fetchLatest() } }.getOrNull() ?: return@launch
            if (info.versionCode > BuildConfig.VERSION_CODE) showUpdateDialog(info)
        }
    }

    fun onResume() {
        val pending = pendingInstall ?: return
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O || activity.packageManager.canRequestPackageInstalls()) {
            pendingInstall = null
            downloadAndInstall(pending)
        }
    }

    private fun showUpdateDialog(info: UpdateInfo) {
        if (activity.isFinishing || dialogVisible) return
        dialogVisible = true
        val message = buildString {
            append("Nova versão: ").append(info.versionName)
            if (info.notes.isNotBlank()) append("\n\n").append(info.notes)
        }
        AlertDialog.Builder(activity)
            .setTitle("Atualização do GranaOK")
            .setMessage(message)
            .setNegativeButton("Depois", null)
            .setPositiveButton("Atualizar agora") { _, _ -> requestInstallPermissionOrDownload(info) }
            .setOnDismissListener { dialogVisible = false }
            .show()
    }

    private fun requestInstallPermissionOrDownload(info: UpdateInfo) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O && !activity.packageManager.canRequestPackageInstalls()) {
            pendingInstall = info
            Toast.makeText(activity, "Ative 'Permitir desta fonte' para o GranaOK e volte ao app.", Toast.LENGTH_LONG).show()
            activity.startActivity(
                Intent(
                    Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,
                    Uri.parse("package:${activity.packageName}"),
                ),
            )
            return
        }
        downloadAndInstall(info)
    }

    private fun downloadAndInstall(info: UpdateInfo) {
        Toast.makeText(activity, "Baixando atualização…", Toast.LENGTH_SHORT).show()
        activity.lifecycleScope.launch {
            val result = runCatching { withContext(Dispatchers.IO) { downloadAndVerify(info) } }
            result.onSuccess(::openInstaller)
                .onFailure {
                    Toast.makeText(
                        activity,
                        "Não foi possível atualizar: ${it.message ?: "falha desconhecida"}",
                        Toast.LENGTH_LONG,
                    ).show()
                }
        }
    }

    private fun fetchLatest(): UpdateInfo {
        val separator = if (BuildConfig.GRANAOK_UPDATE_MANIFEST_URL.contains('?')) '&' else '?'
        val connection = (URL(BuildConfig.GRANAOK_UPDATE_MANIFEST_URL + separator + "ts=" + System.currentTimeMillis())
            .openConnection() as HttpURLConnection).apply {
            requestMethod = "GET"
            connectTimeout = 8_000
            readTimeout = 10_000
            setRequestProperty("Accept", "application/json")
            setRequestProperty("User-Agent", "GranaOK-Android/${BuildConfig.VERSION_NAME}")
            useCaches = false
        }
        try {
            val code = connection.responseCode
            if (code !in 200..299) error("servidor de atualização retornou HTTP $code")
            val json = connection.inputStream.bufferedReader(Charsets.UTF_8).use { it.readText() }
            val obj = JSONObject(json)
            val versionCode = obj.optInt("version_code", 0)
            val versionName = obj.optString("version_name", "")
            val rawUrl = obj.optString("apk_url", "")
            val sha256 = obj.optString("sha256", "").replace(":", "").lowercase()
            val notes = obj.optString("notes", "")
            if (versionCode <= 0 || versionName.isBlank() || rawUrl.isBlank() || sha256.length != 64) {
                error("manifesto de atualização inválido")
            }
            return UpdateInfo(versionCode, versionName, normalizeAndValidateDownloadUrl(rawUrl), sha256, notes)
        } finally {
            connection.disconnect()
        }
    }

    private fun normalizeAndValidateDownloadUrl(raw: String): String {
        val full = if (raw.startsWith("/")) "https://granaok.com.br$raw" else raw
        val uri = URI(full)
        if (uri.scheme != "https" || uri.host?.lowercase() != "granaok.com.br" || !uri.path.startsWith("/apk/")) {
            error("URL de atualização não permitida")
        }
        return full
    }

    private fun downloadAndVerify(info: UpdateInfo): File {
        val dir = File(activity.cacheDir, "updates").apply { mkdirs() }
        val partial = File(dir, "GranaOK-update.apk.part")
        val apk = File(dir, "GranaOK-update.apk")
        partial.delete()
        apk.delete()

        val connection = (URL(info.apkUrl).openConnection() as HttpURLConnection).apply {
            requestMethod = "GET"
            connectTimeout = 10_000
            readTimeout = 60_000
            setRequestProperty("User-Agent", "GranaOK-Android/${BuildConfig.VERSION_NAME}")
            useCaches = false
        }
        try {
            val code = connection.responseCode
            if (code !in 200..299) error("download retornou HTTP $code")
            val length = connection.contentLengthLong
            if (length > MAX_APK_BYTES) error("APK excede o limite permitido")

            val digest = MessageDigest.getInstance("SHA-256")
            var total = 0L
            connection.inputStream.use { input ->
                FileOutputStream(partial).use { output ->
                    val buffer = ByteArray(DEFAULT_BUFFER_SIZE * 4)
                    while (true) {
                        val read = input.read(buffer)
                        if (read <= 0) break
                        total += read
                        if (total > MAX_APK_BYTES) error("APK excede o limite permitido")
                        output.write(buffer, 0, read)
                        digest.update(buffer, 0, read)
                    }
                    output.fd.sync()
                }
            }
            val actualSha = digest.digest().toHex()
            if (!actualSha.equals(info.sha256, ignoreCase = true)) {
                partial.delete()
                error("SHA-256 da atualização não confere")
            }
            if (!partial.renameTo(apk)) error("não foi possível finalizar o download")
            verifyPackageAndSigner(apk)
            return apk
        } finally {
            connection.disconnect()
        }
    }

    private fun verifyPackageAndSigner(apk: File) {
        val pm = activity.packageManager
        val flags = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            PackageManager.GET_SIGNING_CERTIFICATES
        } else {
            @Suppress("DEPRECATION")
            PackageManager.GET_SIGNATURES
        }
        @Suppress("DEPRECATION")
        val archive = pm.getPackageArchiveInfo(apk.absolutePath, flags)
            ?: error("APK baixada não pôde ser validada")
        if (archive.packageName != BuildConfig.APPLICATION_ID) error("APK pertence a outro aplicativo")

        @Suppress("DEPRECATION")
        val installed = pm.getPackageInfo(BuildConfig.APPLICATION_ID, flags)
        val archiveDigests = signerDigests(archive)
        val installedDigests = signerDigests(installed)
        if (archiveDigests.isEmpty() || installedDigests.isEmpty() || archiveDigests.intersect(installedDigests).isEmpty()) {
            error("assinatura da atualização é diferente da assinatura do GranaOK instalado")
        }
    }

    @Suppress("DEPRECATION")
    private fun signerDigests(info: android.content.pm.PackageInfo): Set<String> {
        val signatures = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            val signingInfo = info.signingInfo ?: return emptySet()
            if (signingInfo.hasMultipleSigners()) signingInfo.apkContentsSigners else signingInfo.signingCertificateHistory
        } else {
            info.signatures
        } ?: return emptySet()
        return signatures.map { signature ->
            MessageDigest.getInstance("SHA-256").digest(signature.toByteArray()).toHex()
        }.toSet()
    }

    private fun openInstaller(apk: File) {
        val uri = FileProvider.getUriForFile(activity, "${BuildConfig.APPLICATION_ID}.files", apk)
        val intent = Intent(Intent.ACTION_VIEW).apply {
            setDataAndType(uri, "application/vnd.android.package-archive")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        activity.startActivity(intent)
    }

    private fun ByteArray.toHex(): String = joinToString("") { "%02x".format(it) }

    companion object {
        private const val MAX_APK_BYTES = 200L * 1024L * 1024L
    }
}
