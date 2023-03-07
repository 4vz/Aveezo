
from datetime import datetime


data = {}
# <ip> { <user>: [{ login: <logintime>, config: }, ...]}

def alu(line, act, dt, ip):
    user_start = line.find("[")
    user_end = line.find("]", user_start)
    user_string = line[user_start+1:user_end]

    if ip not in data:
        data[ip] = {}
    node = data[ip]

    if user_string not in node:
        node[user_string] = {}
    user = node[user_string]

    if act == "login":
        for session in user:
            login = session["login"]
            if login > dt:
                # new login probably
                user
        


markers = {
    "USER-MINOR-cli_user_login": [alu, "login"]
    "USER-MINOR-cli_user_logout": [alu, "logout"]
}

def process(line):
    for marker in markers:
        if marker in line:
            detail = markers[marker]
            man = detail[0]
            act = detail[1]
            dt = datetime.strptime(line[0:15], "%b %d %H:%M:%S")
            dt = dt.replace(year=datetime.now().year)
            ip = line[16:line.find(" ", 16)]
            man(line, act, dt, ip)

process("Feb 27 20:08:54 172.30.129.156 Feb 27 20:08:53 172.30.129.156 JKTMED2SPU: 7017634 Base USER-MINOR-cli_config_io-2011 [app-crow-wr1]:  User from 61.94.111.79: ME-D2-SPU>config>service#  epipe 1111")

print(data)