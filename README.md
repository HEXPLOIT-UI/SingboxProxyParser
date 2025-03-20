# SingboxProxyParser

This program allows you to get proxy list in one file from all specified links, it can be public repositories or any other sites that return data in raw text/json format.

To use the program you will need:
1. Install .NET 8 (https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. Specify in the file links.txt, which is located in the files folder, links to the resources from which you want to parse proxy
3. Run program and wait for the end of execution
4. Open the proxies.txt file in the files folder and copy all its contents to the clipboard
5. Insert into a client supporting singbox core and run url-test to filter out non-working proxies

Features:
- the program will sift out repetitive proxies by itself
- support for the following protocols for parsed proxies: vmess|vless|ss|trojan|socks4|socks5|tuic|http|https|hysteria|hysteria2|naive
- Fast, asynchronous and without memoryleaks
