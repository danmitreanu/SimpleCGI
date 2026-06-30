#pragma once

#include <string>
#include <iostream>
#include <map>
#include <vector>
#include <functional>

#define SIMPLE_CGI_IMPLEMENT()						\
int main(int argc, const char* const* const argv) { \
	simple_cgi_main();								\
}

struct simple_cgi_request
{
	std::string req_id;
	std::string method;
	std::string abs_path;
	std::string path;
	std::string query_string;
	std::map<std::string, std::string> query;
	std::map<std::string, std::vector<std::string>> headers;

	std::istream* body_stream{ nullptr };

	std::istream& body() const;
	std::string body_read_string() const;
};

struct simple_cgi_response
{
	struct cookie
	{
		std::string name;
		std::string value;
		std::string path;
		std::string domain;
	};

	std::string req_id;
	int status_code{ 200 };
	std::string content_type{ "application/octet-stream" };
	std::map<std::string, std::vector<std::string>> headers;
	std::vector<cookie> cookies;

	virtual void send(std::ostream& out) const;
};

struct simple_cgi_response_string : simple_cgi_response
{
	std::string body_string;

	void send(std::ostream& out) const override;
};

struct simple_cgi_response_stream : simple_cgi_response
{
	std::function<void(std::ostream&)> body_stream;

	void send(std::ostream& out) const override;
};

void simple_cgi_main();
simple_cgi_request simple_cgi_parse_request(std::istream& in);

// Implement this in your program after SIMPLE_CGI_IMPLEMENT()
void simple_cgi_handle(const simple_cgi_request& request, simple_cgi_response& response);

