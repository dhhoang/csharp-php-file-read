## Compare a program speed between C# and PHP

Environments used:
* EC2 t3-xlarge instance - Amazon Linux 2
* PHP 8.0
* C# - .NET 6

Generate test file with
```sh
base64 /dev/urandom | head -c [file_size] > file.txt
```
