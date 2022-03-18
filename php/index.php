<?php

$file_path="/home/ec2-user/test.txt";
// $file_path="/home/ec2-user/ubuntu-20.04.4-desktop-amd64.iso";
ini_set('memory_limit', '5G');

function test_substr_count()
{
    global $file_path;

    $file = fopen($file_path, 'r');
    $counter = 0;
    $timer = microtime(true);
    while ( ! feof($file)) {
        $buffer = fgets($file, 4096);
        $counter += substr_count($buffer, '1');
    }
    $timer = microtime(true) - $timer;
    fclose($file);
    printf("counted %s 1s in %s milliseconds\n", number_format($counter), number_format($timer * 1000, 4));
}

function test_manual_count()
{
    global $file_path;

    $file = fopen($file_path, 'r');
    $counter = 0;
    $timer = microtime(true);
    while ( ! feof($file)) {
        $buffer = fgets($file, 4096);
        $length = strlen($buffer);
        for ($i = 0; $i < $length; $i++) {
            if($buffer[$i]=='1'){
                $counter += 1;
            }
        }
    }
    $timer = microtime(true) - $timer;
    fclose($file);
    printf("counted %s 1s in %s milliseconds\n", number_format($counter), number_format($timer * 1000, 4));
}

for ($i = 0; $i < 10; $i++){
    test_substr_count();
}
