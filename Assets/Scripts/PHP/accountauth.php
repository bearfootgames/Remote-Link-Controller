<?php
$user = $_POST["username"];
$pass = $_POST["password"];
$type = $_POST["type"];
$device = $_POST["deviceid"];
$receipt = $_POST["receipt"];

$PREVauthoz = $_POST["PREVauthoz"];
$NEWauthoz = $_POST["NEWauthoz"];

if($type == "createnew"){

    $structure = "./Accounts/".$user;
    
    if (!mkdir($structure, 0777, true)) {
        die('ERROR cannot create directory U');
    }

    $myfile = fopen("Accounts/".$user."/password.txt", "w") or die("ERROR cannot create file P");
    $txt = $pass;
    fwrite($myfile, $txt);
    fclose($myfile);

    $myfile = fopen("Accounts/".$user."/devices.txt", "w") or die("ERORR cannot create file D");
    $txt = "0,";
    fwrite($myfile, $txt);
    fclose($myfile);

    $myfile = fopen("Accounts/".$user."/receipts.txt", "w") or die("ERORR cannot create file D");
    $txt = "";
    fwrite($myfile, $txt);
    fclose($myfile);
    
    $d = file_get_contents("Accounts/".$user."/devices.txt");
    echo $d;
}else{
    $p = file_get_contents("Accounts/".$user."/password.txt");
    if ($p == $pass){
        $d = file_get_contents("Accounts/".$user."/devices.txt");
        
        if($type == "signin"){echo $d;}
    }else{
        echo 'ERROR sign in failed: ';
        exit();
    }
}

if($type == "adddevice"){
    $d = file_get_contents("Accounts/".$user."/devices.txt");
    $newD = $d.$device.",";

    $myfile = fopen("Accounts/".$user."/devices.txt", "w") or die("ERORR cannot create file D");
    fwrite($myfile, $newD);
    fclose($myfile);

    echo file_get_contents("Accounts/".$user."/devices.txt");
}
if($type == "removedevice"){
    $d = file_get_contents("Accounts/".$user."/devices.txt");
    $newD = str_replace($device.",","",$d);
    $myfile = fopen("Accounts/".$user."/devices.txt", "w") or die("ERORR cannot create file D");
    fwrite($myfile, $newD);
    fclose($myfile);
    
    echo file_get_contents("Accounts/".$user."/devices.txt");
}
if($type == "authoz"){

    $devz = file_get_contents("Accounts/".$user."/devices.txt");
    $arr = explode(",",$devz);

    if($arr[0] == $PREVauthoz){

        $i = (int)$NEWauthoz+(int)$arr[0];
        $arr[0] = $i;

        $txt = implode(",",$arr);

        $myfile = fopen("Accounts/".$user."/devices.txt", "w") or die("ERORR cannot create file D");
        fwrite($myfile, $txt);
        fclose($myfile);

        echo file_get_contents("Accounts/".$user."/devices.txt");
    }else{
        echo 'ERROR authorization error';
    }
}
if($type == "receipt"){
    
    $d = file_get_contents("Accounts/".$user."/receipts.txt");
    $n = $d.$receipt.",";

    $myfile = fopen("Accounts/".$user."/receipts.txt", "w") or die("ERORR cannot create file D");
    fwrite($myfile, $n);
    fclose($myfile);

    echo file_get_contents("Accounts/".$user."/receipts.txt");
}
if($type == "echoreceipt"){

    echo file_get_contents("Accounts/".$user."/receipts.txt");
}
?>