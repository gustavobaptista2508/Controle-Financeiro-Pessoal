from pathlib import Path

root = Path('native-build/GranaOK_Android_Native_v0_2')

network = root / 'app/src/main/java/br/com/granaok/app/data/remote/NetworkClient.kt'
s = network.read_text()
s = s.replace('// O backend atual restringe /api/action ao cliente web.\n            // O cookie gk_session continua sendo a autenticação real.', '// O backend identifica o cliente nativo pelo header; o cookie gk_session continua sendo a autenticação real.')
s = s.replace('.header("X-GranaOk-Client", "web")', '.header("X-GranaOk-Client", "android")')
network.write_text(s)

build = root / 'app/build.gradle.kts'
s = build.read_text()
s = s.replace('versionCode = 2\n        versionName = "0.2.0-native-api"', 'versionCode = 5\n        versionName = "0.2.3-native"')
api_line = '        buildConfigField("String", "GRANAOK_API_BASE_URL", "\\\"${config("GRANAOK_API_BASE_URL", "https://granaok.com.br/")}\\\"")'
if api_line not in s:
    raise SystemExit('API BuildConfig line not found')
s = s.replace(api_line, api_line + '\n        buildConfigField("String", "GRANAOK_UPDATE_MANIFEST_URL", "\\\"https://granaok.com.br/apk/latest.json\\\"")')
build.write_text(s)

manifest = root / 'app/src/main/AndroidManifest.xml'
s = manifest.read_text()
s = s.replace('    <uses-permission android:name="android.permission.USE_BIOMETRIC" />', '    <uses-permission android:name="android.permission.USE_BIOMETRIC" />\n    <uses-permission android:name="android.permission.REQUEST_INSTALL_PACKAGES" />')
provider = '''\n        <provider\n            android:name="androidx.core.content.FileProvider"\n            android:authorities="${applicationId}.files"\n            android:exported="false"\n            android:grantUriPermissions="true">\n            <meta-data\n                android:name="android.support.FILE_PROVIDER_PATHS"\n                android:resource="@xml/file_paths" />\n        </provider>\n\n'''
s = s.replace('        <activity\n            android:name=".MainActivity"', provider + '        <activity\n            android:name=".MainActivity"')
manifest.write_text(s)

xml = root / 'app/src/main/res/xml/file_paths.xml'
xml.parent.mkdir(parents=True, exist_ok=True)
xml.write_text('''<?xml version="1.0" encoding="utf-8"?>\n<paths xmlns:android="http://schemas.android.com/apk/res/android">\n    <cache-path name="updates" path="updates/" />\n</paths>\n''')

main = root / 'app/src/main/java/br/com/granaok/app/MainActivity.kt'
s = main.read_text()
s = s.replace('import br.com.granaok.app.ui.GranaOkApp', 'import br.com.granaok.app.ui.GranaOkApp\nimport br.com.granaok.app.update.AppUpdateManager')
s = s.replace('class MainActivity : FragmentActivity() {', 'class MainActivity : FragmentActivity() {\n    private lateinit var updateManager: AppUpdateManager')
s = s.replace('        val container = (application as GranaOkApplication).container', '        val container = (application as GranaOkApplication).container\n        updateManager = AppUpdateManager(this)')
needle = '''        }\n    }\n}\n'''
replacement = '''        }\n\n        updateManager.checkAtStartup()\n    }\n\n    override fun onResume() {\n        super.onResume()\n        if (::updateManager.isInitialized) updateManager.onResume()\n    }\n}\n'''
if needle not in s:
    raise SystemExit('MainActivity closing block not found')
s = s.replace(needle, replacement)
main.write_text(s)

src = Path('.build-native-v02/AppUpdateManager.kt')
dst = root / 'app/src/main/java/br/com/granaok/app/update/AppUpdateManager.kt'
dst.parent.mkdir(parents=True, exist_ok=True)
dst.write_text(src.read_text())

# Overlay da v0.2.2: Grana IA nativa, leitura compatível com VPS antiga e UI refinada.
import base64
import hashlib
import zipfile
parts = sorted(Path('.build-native-v02').glob('v022.b64.*'))
if len(parts) != 5:
    raise SystemExit(f'Esperava 5 chunks v022, encontrei {len(parts)}')
encoded = ''.join(p.read_text().strip() for p in parts)
patch_zip = Path('/tmp/v022_patch.zip')
patch_zip.write_bytes(base64.b64decode(encoded))
expected_sha = '374c118b0f8b74d617b2ee4bc1e63ce51d3b4d9bc424c2d832af843d92c94b97'
actual_sha = hashlib.sha256(patch_zip.read_bytes()).hexdigest()
if actual_sha != expected_sha:
    raise SystemExit(f'SHA v022 inválido: {actual_sha}')
with zipfile.ZipFile(patch_zip) as z:
    bad = z.testzip()
    if bad:
        raise SystemExit(f'ZIP v022 corrompido em: {bad}')
    z.extractall(root)

# Compose API 35 compatibility: weight is a scoped Row/Column extension and must not be imported directly.
for p in (root / 'app/src/main/java').rglob('*.kt'):
    text_value = p.read_text()
    if 'import androidx.compose.foundation.layout.weight\\n' in text_value:
        p.write_text(text_value.replace('import androidx.compose.foundation.layout.weight\\n', ''))


# Robust cleanup for Compose API 35: overlay files may use different line endings.
for p in (root / 'app/src/main/java').rglob('*.kt'):
    text_value = p.read_text()
    cleaned = text_value.replace('import androidx.compose.foundation.layout.weight\\r\\n', '')
    cleaned = cleaned.replace('import androidx.compose.foundation.layout.weight\\n', '')
    cleaned = cleaned.replace('import androidx.compose.foundation.layout.weight', '')
    if cleaned != text_value:
        p.write_text(cleaned)


# v0.2.3: contraste consistente, gravação compatível, screenshots e ícone da marca.
theme = root / 'app/src/main/java/br/com/granaok/app/ui/theme/Theme.kt'
theme_text = theme.read_text()
theme_text = theme_text.replace(
    'colorScheme = if (isSystemInDarkTheme()) Dark else Light',
    'colorScheme = Light',
)
theme.write_text(theme_text)

network = root / 'app/src/main/java/br/com/granaok/app/data/remote/NetworkClient.kt'
network_text = network.read_text()
network_text = network_text.replace(
    '"knowledge_summary",\n    )',
    '"knowledge_summary",\n        "transaction_save",\n        "transaction_status",\n    )',
)
network_text = network_text.replace(
    'Somente ações de leitura/IA\n * podem repetir uma vez como \`web\` quando o servidor responde explicitamente\n * "Cliente inválido". Escritas nunca usam fallback, para não registrar source=web.',
    'Ações de leitura/IA e lançamentos podem repetir uma vez como \`web\` quando o servidor\n * antigo responde explicitamente "Cliente inválido". No backend novo, Android é usado direto.',
)
network.write_text(network_text)

dash = root / 'app/src/main/java/br/com/granaok/app/ui/dashboard/DashboardScreen.kt'
dash_text = dash.read_text()
dash_text = dash_text.replace(
    'Leitura e Grana IA estão usando o cliente Web temporariamente. Novos lançamentos Android só sincronizam depois da atualização do backend na VPS.',
    'A VPS ainda está no backend anterior. Leitura, Grana IA e lançamentos usam compatibilidade temporária; após atualizar o backend, a origem volta automaticamente para Android.',
)
dash.write_text(dash_text)

main = root / 'app/src/main/java/br/com/granaok/app/MainActivity.kt'
main_text = main.read_text()
if 'import android.view.WindowManager' not in main_text:
    main_text = main_text.replace('import android.os.Bundle', 'import android.os.Bundle\nimport android.view.WindowManager')
if 'window.clearFlags(WindowManager.LayoutParams.FLAG_SECURE)' not in main_text:
    main_text = main_text.replace(
        'super.onCreate(savedInstanceState)',
        'super.onCreate(savedInstanceState)\n        window.clearFlags(WindowManager.LayoutParams.FLAG_SECURE)',
        1,
    )
main.write_text(main_text)

manifest = root / 'app/src/main/AndroidManifest.xml'
manifest_text = manifest.read_text()
if 'android:icon="@drawable/ic_granaok"' not in manifest_text:
    manifest_text = manifest_text.replace(
        '<application',
        '<application\n        android:icon="@drawable/ic_granaok"\n        android:roundIcon="@drawable/ic_granaok"',
        1,
    )
manifest.write_text(manifest_text)

icon = root / 'app/src/main/res/drawable/ic_granaok.xml'
icon.parent.mkdir(parents=True, exist_ok=True)
icon.write_text('''<?xml version="1.0" encoding="utf-8"?>
<vector xmlns:android="http://schemas.android.com/apk/res/android"
    android:width="108dp"
    android:height="108dp"
    android:viewportWidth="108"
    android:viewportHeight="108">
    <path
        android:fillColor="#17A673"
        android:pathData="M18,4H90C97.7,4 104,10.3 104,18V90C104,97.7 97.7,104 90,104H18C10.3,104 4,97.7 4,90V18C4,10.3 10.3,4 18,4Z" />
    <path
        android:fillColor="@android:color/transparent"
        android:strokeColor="#FFFFFF"
        android:strokeWidth="9"
        android:strokeLineCap="round"
        android:strokeLineJoin="round"
        android:pathData="M29,56 L47,73 L80,36" />
</vector>
''')


# Overlay v0.2.4: Play/sideload, API 36, safe insets, cards and transaction UX.
v024_parts = sorted(Path('.build-native-v02').glob('v024.b64.*'))
if len(v024_parts) != 7:
    raise SystemExit(f'Esperava 7 chunks v024, encontrei {len(v024_parts)}')
v024_encoded = ''.join(p.read_text().strip() for p in v024_parts)
v024_zip = Path('/tmp/v024_patch.zip')
v024_zip.write_bytes(base64.b64decode(v024_encoded))
v024_expected_sha = '4d12731802a876cdf0de39b463bb055a362d375836d475f0d5948a5210199b7d'
v024_actual_sha = hashlib.sha256(v024_zip.read_bytes()).hexdigest()
if v024_actual_sha != v024_expected_sha:
    raise SystemExit(f'SHA v024 inválido: {v024_actual_sha}')
with zipfile.ZipFile(v024_zip) as z:
    bad = z.testzip()
    if bad:
        raise SystemExit(f'ZIP v024 corrompido em: {bad}')
    z.extractall(root)

# Reapply Compose compatibility cleanup after the v0.2.4 overlay.
for p in (root / 'app/src/main/java').rglob('*.kt'):
    text_value = p.read_text()
    cleaned = text_value.replace('import androidx.compose.foundation.layout.weight\\r\\n', '')
    cleaned = cleaned.replace('import androidx.compose.foundation.layout.weight\\n', '')
    cleaned = cleaned.replace('import androidx.compose.foundation.layout.weight', '')
    if cleaned != text_value:
        p.write_text(cleaned)

# Final SDK level for the compatible Play/internal-test toolchain.
build = root / 'app/build.gradle.kts'
build_text = build.read_text().replace('compileSdk = 37', 'compileSdk = 36').replace('targetSdk = 37', 'targetSdk = 36')
build.write_text(build_text)

# The real updater exists only in the sideload flavor. Play gets a no-op implementation,
# so the Play bundle contains neither update behavior nor REQUEST_INSTALL_PACKAGES.
stale_updater = root / 'app/src/main/java/br/com/granaok/app/update/AppUpdateManager.kt'
sideload_updater = root / 'app/src/sideload/java/br/com/granaok/app/update/AppUpdateManager.kt'
play_updater = root / 'app/src/play/java/br/com/granaok/app/update/AppUpdateManager.kt'
sideload_updater.parent.mkdir(parents=True, exist_ok=True)
play_updater.parent.mkdir(parents=True, exist_ok=True)
if stale_updater.exists():
    sideload_updater.write_text(stale_updater.read_text())
elif Path('.build-native-v02/AppUpdateManager.kt').exists():
    sideload_updater.write_text(Path('.build-native-v02/AppUpdateManager.kt').read_text())
else:
    raise SystemExit('Updater sideload não encontrado.')
play_updater.write_text('''package br.com.granaok.app.update

import androidx.fragment.app.FragmentActivity

class AppUpdateManager(private val activity: FragmentActivity) {
    fun checkAtStartup() = Unit
    fun onResume() = Unit
}
''')
if stale_updater.exists():
    stale_updater.unlink()


# Material3 TopAppBar is experimental in the pinned compatible Compose stack.
for rel, fn_name in [
    ('app/src/main/java/br/com/granaok/app/ui/transactions/NewTransactionScreen.kt', 'NewTransactionScreen'),
    ('app/src/main/java/br/com/granaok/app/ui/transactions/TransactionsScreen.kt', 'TransactionsScreen'),
]:
    p = root / rel
    text_value = p.read_text()
    if 'import androidx.compose.material3.ExperimentalMaterial3Api' not in text_value:
        text_value = text_value.replace(
            'import androidx.compose.material3.TopAppBar',
            'import androidx.compose.material3.TopAppBar\nimport androidx.compose.material3.ExperimentalMaterial3Api',
        )
    target = '@Composable\nfun ' + fn_name
    if '@OptIn(ExperimentalMaterial3Api::class)\n' + target not in text_value:
        text_value = text_value.replace(
            target,
            '@OptIn(ExperimentalMaterial3Api::class)\n' + target,
            1,
        )
    p.write_text(text_value)

print('Patch applied')
