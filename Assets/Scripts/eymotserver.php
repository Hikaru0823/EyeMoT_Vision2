<?php
// server_api.php

header('Content-Type: text/plain; charset=UTF-8');

$filePath = __DIR__ . '/server_list.csv';
$csvHeader = ['created_at', 'ip', 'port', 'password', 'global_ip'];

$action = $_POST['action'] ?? $_GET['action'] ?? '';

if ($action === '') {
    http_response_code(400);
    echo 'Error: action is required';
    exit;
}

function getClientGlobalIp(): string
{
    $candidates = [
        $_SERVER['HTTP_CF_CONNECTING_IP'] ?? '',
        $_SERVER['HTTP_X_REAL_IP'] ?? '',
        $_SERVER['HTTP_X_FORWARDED_FOR'] ?? '',
        $_SERVER['REMOTE_ADDR'] ?? '',
    ];

    foreach ($candidates as $candidate) {
        foreach (explode(',', $candidate) as $ip) {
            $ip = trim($ip);
            if (filter_var($ip, FILTER_VALIDATE_IP)) {
                return $ip;
            }
        }
    }

    return '';
}

function normalizeRow(array $row): array
{
    return [
        $row[0] ?? '',
        $row[1] ?? '',
        $row[2] ?? '',
        $row[3] ?? '',
        $row[4] ?? '',
    ];
}

function readCsvData($file, array $header): array
{
    rewind($file);

    $rows = [];
    $isFirstRow = true;

    while (($row = fgetcsv($file)) !== false) {
        if ($isFirstRow) {
            $isFirstRow = false;
            if (($row[0] ?? '') === 'created_at') {
                continue;
            }
        }

        if (count($row) === 1 && ($row[0] ?? '') === '') {
            continue;
        }

        $rows[] = normalizeRow($row);
    }

    return array_merge([$header], $rows);
}

function writeCsvData($file, array $rows): void
{
    rewind($file);
    ftruncate($file, 0);

    foreach ($rows as $row) {
        fputcsv($file, $row);
    }

    fflush($file);
}

if (!file_exists($filePath)) {
    $file = fopen($filePath, 'w');
    if ($file === false) {
        http_response_code(500);
        echo 'Error: could not create CSV file';
        exit;
    }
    fputcsv($file, $csvHeader);
    fclose($file);
}

// Get client global IP address.
if ($action === 'global_ip') {
    echo getClientGlobalIp();
    exit;
}

// Add server.
if ($action === 'add') {
    $ip = $_POST['ip'] ?? '';
    $port = $_POST['port'] ?? '';
    $password = $_POST['password'] ?? '';
    $globalIp = getClientGlobalIp();

    if ($ip === '' || $port === '' || $password === '') {
        http_response_code(400);
        echo 'Error: ip, port, and password are required';
        exit;
    }

    if (!ctype_digit($port)) {
        http_response_code(400);
        echo 'Error: port must be numeric';
        exit;
    }

    $file = fopen($filePath, 'c+');

    if ($file === false) {
        http_response_code(500);
        echo 'Error: could not open CSV file';
        exit;
    }

    if (flock($file, LOCK_EX)) {
        $rows = readCsvData($file, $csvHeader);
        $filteredRows = [$csvHeader];
        $deletedCount = 0;

        for ($i = 1; $i < count($rows); $i++) {
            $rowIp = $rows[$i][1] ?? '';
            $rowGlobalIp = $rows[$i][4] ?? '';

            if ($rowIp === $ip || ($globalIp !== '' && $rowGlobalIp === $globalIp)) {
                $deletedCount++;
                continue;
            }

            $filteredRows[] = $rows[$i];
        }

        $filteredRows[] = [
            date('Y-m-d H:i:s'),
            $ip,
            $port,
            $password,
            $globalIp,
        ];

        writeCsvData($file, $filteredRows);
        flock($file, LOCK_UN);

        echo 'OK: added deleted ' . $deletedCount . ' duplicates';
    } else {
        http_response_code(500);
        echo 'Error: could not lock CSV file';
    }

    fclose($file);
    exit;
}

// Delete server.
if ($action === 'delete') {
    $ip = $_POST['ip'] ?? '';
    $port = $_POST['port'] ?? '';
    $password = $_POST['password'] ?? '';

    if ($ip === '' || $port === '' || $password === '') {
        http_response_code(400);
        echo 'Error: ip, port, and password are required';
        exit;
    }

    $file = fopen($filePath, 'c+');

    if ($file === false) {
        http_response_code(500);
        echo 'Error: could not open CSV file';
        exit;
    }

    if (flock($file, LOCK_EX)) {
        $rows = readCsvData($file, $csvHeader);
        $filteredRows = [$csvHeader];
        $deletedCount = 0;

        for ($i = 1; $i < count($rows); $i++) {
            $rowIp = $rows[$i][1] ?? '';
            $rowPort = $rows[$i][2] ?? '';
            $rowPassword = $rows[$i][3] ?? '';

            if ($rowIp === $ip && $rowPort === $port && $rowPassword === $password) {
                $deletedCount++;
                continue;
            }

            $filteredRows[] = $rows[$i];
        }

        writeCsvData($file, $filteredRows);
        flock($file, LOCK_UN);

        echo 'OK: deleted ' . $deletedCount;
    } else {
        http_response_code(500);
        echo 'Error: could not lock CSV file';
    }

    fclose($file);
    exit;
}

// List servers.
if ($action === 'list') {
    $csv = file_get_contents($filePath);

    if ($csv === false) {
        http_response_code(500);
        echo 'Error: could not read CSV file';
        exit;
    }

    echo $csv;
    exit;
}

http_response_code(400);
echo 'Error: unknown action';
exit;
?>
