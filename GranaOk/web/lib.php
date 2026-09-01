<?php
declare(strict_types=1);

if (session_status() !== PHP_SESSION_ACTIVE) {
    $secure = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off');
    session_set_cookie_params([
        'lifetime' => 0,
        'path' => '/',
        'secure' => $secure,
        'httponly' => true,
        'samesite' => 'Lax',
    ]);
    session_start();
}

function cfg(): array {
    static $cfg;
    if ($cfg) return $cfg;
    $path = __DIR__ . '/config.local.php';
    if (!is_file($path)) {
        throw new RuntimeException('Configuração ausente. Copie config.example.php para config.local.php.');
    }
    $cfg = require $path;
    if (!is_array($cfg) || empty($cfg['db'])) throw new RuntimeException('Configuração inválida.');
    return $cfg;
}

function db(): PDO {
    static $pdo;
    if ($pdo instanceof PDO) return $pdo;
    $c = cfg()['db'];
    $host = (string)($c['host'] ?? '');
    $port = (int)($c['port'] ?? 3306);
    $name = (string)($c['name'] ?? '');
    $user = (string)($c['user'] ?? '');
    $pass = (string)($c['password'] ?? '');
    $charset = (string)($c['charset'] ?? 'utf8mb4');
    if ($host === '' || $name === '' || $user === '') throw new RuntimeException('Configuração MySQL incompleta.');
    $dsn = "mysql:host={$host};port={$port};dbname={$name};charset={$charset}";
    $pdo = new PDO($dsn, $user, $pass, [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
        PDO::ATTR_EMULATE_PREPARES => false,
    ]);
    return $pdo;
}

function prefix(): string {
    $p = (string)(cfg()['db']['prefix'] ?? 'granaok_');
    return preg_match('/^[A-Za-z0-9_]{1,32}$/', $p) ? $p : 'granaok_';
}

function input_json(): array {
    $raw = file_get_contents('php://input');
    if (!$raw) return [];
    $data = json_decode($raw, true);
    return is_array($data) ? $data : [];
}

function respond(array $data, int $status = 200): never {
    http_response_code($status);
    header('Content-Type: application/json; charset=utf-8');
    header('Cache-Control: no-store');
    echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

function require_auth(): void {
    if (empty($_SESSION['granaok_web_auth'])) respond(['ok'=>false,'error'=>'Sessão expirada.'], 401);
}

function safe_month(?string $month): string {
    return ($month && preg_match('/^\d{4}-(0[1-9]|1[0-2])$/', $month)) ? $month : date('Y-m');
}

function safe_date(?string $date, ?string $fallback = null): string {
    $date = $date ?: ($fallback ?: date('Y-m-d'));
    $d = DateTime::createFromFormat('Y-m-d', $date);
    if (!$d || $d->format('Y-m-d') !== $date) throw new InvalidArgumentException('Data inválida.');
    return $date;
}

function money($v): float {
    if (is_string($v)) {
        $v = trim($v);
        if (strpos($v, ',') !== false) $v = str_replace(',', '.', str_replace('.', '', $v));
    }
    $n = (float)$v;
    if ($n < 0) $n = abs($n);
    return round($n, 2);
}

function category_id(PDO $db, string $name, string $kind): int {
    $p = prefix();
    $name = trim($name) ?: 'Outros';
    $st = $db->prepare("INSERT IGNORE INTO {$p}categories(name,kind,active) VALUES(?,?,1)");
    $st->execute([$name,$kind]);
    $st = $db->prepare("SELECT id FROM {$p}categories WHERE name=? AND kind=? LIMIT 1");
    $st->execute([$name,$kind]);
    return (int)$st->fetchColumn();
}

function ensure_web_columns(PDO $db): void {
    $p = prefix();
    $columns = [
        ["{$p}card_invoices",'paid_date','DATE NULL'],
        ["{$p}transactions",'observations','LONGTEXT NULL'],
        ["{$p}transactions",'installment_group','VARCHAR(64) NULL'],
        ["{$p}transactions",'installment_number','INT NOT NULL DEFAULT 1'],
        ["{$p}transactions",'installment_total','INT NOT NULL DEFAULT 1'],
    ];
    foreach ($columns as [$table,$column,$def]) {
        try { $db->exec("ALTER TABLE {$table} ADD COLUMN {$column} {$def}"); }
        catch (PDOException $e) { if ((int)($e->errorInfo[1] ?? 0) !== 1060) throw $e; }
    }
}

function invoice_due_date(string $month, int $dueDay): string {
    $d = new DateTime($month . '-01');
    $last = (int)$d->format('t');
    $d->setDate((int)$d->format('Y'), (int)$d->format('m'), min(max(1,$dueDay),$last));
    return $d->format('Y-m-d');
}

function sync_invoice(PDO $db, int $cardId, string $month, string $dueDate): void {
    $p = prefix();
    $st = $db->prepare("SELECT COALESCE(SUM(amount),0) FROM {$p}card_purchases WHERE card_id=? AND DATE_FORMAT(due_date,'%Y-%m')=?");
    $st->execute([$cardId,$month]);
    $total = (float)$st->fetchColumn();
    $st = $db->prepare("SELECT id,status,paid_date FROM {$p}card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? ORDER BY id LIMIT 1");
    $st->execute([$cardId,$month]);
    $row = $st->fetch();
    if ($row) {
        $st = $db->prepare("UPDATE {$p}card_invoices SET amount=?,due_date=? WHERE id=?");
        $st->execute([$total,$dueDate,$row['id']]);
    } else {
        $st = $db->prepare("INSERT INTO {$p}card_invoices(card_id,reference_month,due_date,amount,status) VALUES(?,?,?,?, 'open')");
        $st->execute([$cardId,$month.'-01',$dueDate,$total]);
    }
}
