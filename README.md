# SimpleCGI

<mark>DISCLAIMER</mark>

_SimpleCGI is built to serve web pages quickly and easily. It is best used for internal tooling or demos._

_Alcohol consumption was employed during the writing of this piece of software, so you are solely responsible of the consequences of using this magnificent, non-AI enhanced marvel of engineering._

## Use cases

SimpleCGI was built to facillitate quick deployment of simple web pages and functionality, allowing you to run custom code via the good old CGI method or to simply serve any kind of static file. It focuses on allowing a simple drag and drop or copy-paste deployment routine.

For example:
- Serving a compiled frontend application, such as React
- Implementing custom endpoints that can run any kind of desired code with EXE endpoints (CGI method, old school PHP style)
- Simply serving files from a directory
- Of course, all of the above, behind the same server

## Deployment

Compile or download a `SimpleCGI.WebServer` executable, and launch it with `{url} {wwwroot}`:

Powershell:

```
.\SimpleCGI.WebServer.exe http://localhost:8080/ C:\wwwroot
```

Linux or whatever:

```
./SimpleCGI.WebServer http://localhost:8080/ /var/wwwroot
```

It uses .NET's `HttpListener`. Old and bad, but works, so don't care. Fits the purpose. Just make sure you have the necessary permissions, it will bind to the port you specified when launching.

## Architecture

Not really in mind. Currently single-threaded, but each request is very self-contained, can be easily parallelized. Will probably do this in the future. This is the one big drawback.

## HOW TO RUN

Not much to do, look at the deployment section.

The `wwwroot` directory is important. Here's the gist of it:
- Every subdirectory will have a `_simple.json` file, including the root. If none is specified, it is considered to be a simple file-serving directory.
- The directories are considered the routing of the website. `wwwroot/test/path` is essentially `http://localhost:8080/test/path`. Keep in mind, `index.html` is not served by default currently.

### `_simple.json`

```
{
    "file?": {
        "path": "file or directory",
        "contentType?": "image/png
    },
    "forwardPaths?": "true or false (bool, not string), default false",
    "exe?": "path or name of executable from PATH",
    "arguments?": [
        "arguments to pass to exe"
    ]
}
```

Everything with `?` can be ommitted. Well, everything can be omitted. Like you can literally not have the file. The default, file-serving from directory, inferred `_simple.json` is:

```
{
    "file": {}
}
```

This means that `path` is `.` (the current directory) and `contentType` is `null`, meaning it will be inferred from the extension. Not many extensions are supported now. "Unsupported" extensions will just spit out `application/octet-stream`. For those, just specify a `contentType`.

### Examples for `_simple.json`

You can look at `SimpleCGI.WebServer/wwwroot` for a somewhat comprehensive example. Even better, install [`Bruno`](https://www.usebruno.com/) and open the collection inside `Bruno` of this repo.

Not going to? Fine. Go on.

`{host}` will be your defined endpoint, `http://localhost:8080` or whatever.

#### Serving an image from a certain path

Say you want to serve `my_cat.png` from `{host}/endpoint/best_cat_ever`. You can do one of these 3, in order of more weird to less weird:

```
| wwwroot
    | endpoint
        | best_cat_ever
            | _simple.json
            | my_cat.png

{
    "file": {
        "path": "my_cat.png",
        "contentType": "image/png"
    }
}
```

or

```
| wwwroot
    | endpoint
        | _simple.json
        | best_cat_ever

{
    "file": {
        "contentType": "image/png"
    }
}
```

This one's plain weird because best_cat_ever is the PNG file served from a file directory, but, like, it has no extension you weirdo.

Anyway, or

```
| wwwroot
    | _simple.json
    | my_cat.png

{
    "file": {
        "my_cat.png",
        "contentType": "image.png"
    },
    "forwardPaths": true
}
```

This one's the weirdest. It will work. But so will `{host}/endpoint/worst_cat_ever` (or even `{host}/worst_cats_catalog/the_worst.PnG`), because of `forwardPaths`. Basically, when `forwardPaths` is true, every sub-path under that will still serve your desired file or EXE. This is good for cases like React, where one would want `{host}/something/else` to still go to the `index.html` stored next to `_simple.json` (given you serve `index.html` from it).

#### Executables via "CGI"

From within an executable you can literally return anything. You can write headers, the response body, everything.

```
| wwwroot
    | api
        | _simple.json
        | api.exe

{
    "exe": "api.exe",
    "arguments": ["--external_api_key", "XXX"],
    "forwardPaths": true
}
```

Now, `{host}/api` or `{host}/api/literally/anything` will go to `api.exe` which will be launched by the server with the given arguments. If you write the .EXE using the C++ simple_cgi library in `libs/cpp`, this is what the `simple_cgi_request` object will look like when POSTing `{host}/api/some/path?query=string&key=value`:

```cpp
struct simple_cgi_request
{
    std::string req_id = "you don't really care";
    std::string method = "post";
    std::string abs_path = "/api/some/path";
    std::string path = "/some/path"; // note, this is because of forwardPaths
    std::map<std::string, std::string> query = {
        { "query", "string" },
        { "key", "value" }
    };
    std::map<std::string, std::vector<std::string>> headers; // you get the point
}
```

If you don't `forwardPaths`, only `{host}/api` will execute. Everything under will be 404, unless you create other directories in there with their own `_simple.json` config, of course.

## CGI executables protocol

It's quite simple, written drunk at night. Next day, a simple glance at `Executor.cs` helped me write the C++ CGI lib.

It works via STDIN and STDOUT. The server launches your executable and sends the request via STDIN.

```
REQ_ID you_dont_really_care
MTD post
ABS_PATH /api/literally/anything
PATH /literally/anything
QUERY_S ?query=string&key=value
QUERY query string
QUERY key value
HEADER Accept-Encoding br
HEADER Accept-Encoding */*
HEADER Host example.com

this is the request body, can be binary as well
```

Note the empty line.

Your executable will **VERY KINDLY** write back something like the following to standard out:

```
REQI_ID now_you_care_give_it_back
STATUS 301
TYPE text/html
COOKIE session cookie_value
COOKIE_PATH session2 / cookie_value
COOKIE_PATH_DOMAIN session3 / example.com cookie_value
HEADER Location https://example.com

<h1>Redirect</h1>
```

If you write a common library for some language, you are now in a contractual obligation to submit a PR by reading this very text. Thanks.
