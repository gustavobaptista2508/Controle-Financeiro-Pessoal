from pathlib import Path

root = Path('native-build/GranaOK_Android_Native_v0_2')

network = root / 'app/src/main/java/br/com/granaok/app/data/remote/NetworkClient.kt'
s = network.read_text()
s = s.replace('// O backend atual restringe /api/action ao cliente web.\n            // O cookie gk_session continua sendo a autenticação real.', '// O backend identifica o cliente nativo pelo header; o cookie gk_session continua sendo a autenticação real.')
s = s.replace('.header("X-GranaOk-Client", "web")', '.header("X-GranaOk-Client", "android")')
network.write_text(s)

build = root / 'app/build.gradle.kts'
s = build.read_text()
s = s.replace('versionCode = 2\n        versionName = "0.2.0-native-api"', 'versionCode = 4\n        versionName = "0.2.2-native"')
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

print('Patch applied')
