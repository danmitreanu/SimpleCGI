
import json

query = {}
headers = {}

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
            header_name = toks[1]
            header_value = " ".join(toks[2:])
            if not header_name in headers:
                headers[header_name] = [header_value]
            else:
                headers[header_name].append(header_value)

print(f"REQ_ID {req_id}")
print("STATUS 200")
print("TYPE application/json")
print("COOKIE session 103")
print("COOKIE session2 104")
print()

body = json.dumps({
    "query": query,
    "headers": headers,
    "req_id": req_id,
    "method": method,
    "abs_path": abs_path,
    "path": path,
    "query_s": query_string
})
print(body)
