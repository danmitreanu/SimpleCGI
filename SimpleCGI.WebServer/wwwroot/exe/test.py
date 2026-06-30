
import json

query = {}
headers = {}
cookies = {}

while True:
    data = input()
    if data == "":
        break

    toks = data.split(" ")
    match toks[0]:
        case "REQ_ID":
            req_id = toks[1]
        case "MTD":
            method = toks[1]
        case "ABS_PATH":
            abs_path = toks[1]
        case "PATH":
            path = toks[1]
        case "QUERY_S":
            query_string = toks[1]
        case "QUERY":
            query[toks[1]] = " ".join(toks[2:])
        case "HEADER":
            headers[toks[1]] = " ".join(toks[2:])

print("STATUS 200")
print("TYPE application/json")
print()

body = json.dumps({
    "query": query,
    "headers": headers,
    "cookies": cookies,
    "req_id": req_id,
    "method": method,
    "abs_path": abs_path,
    "path": path,
    "query_s": query_string
})
print(body)
