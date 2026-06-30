#include <simplecgi.h>

#include <sstream>

std::istream& simple_cgi_request::body() const
{
	return *body_stream;
}

std::string simple_cgi_request::body_read_string() const
{
	return std::string(std::istreambuf_iterator<char>(body()), {});
}

void simple_cgi_response::send(std::ostream& out) const
{
	out << "REQ_ID " << req_id << '\n'
		<< "STATUS " << status_code << '\n'
		<< "TYPE " << content_type << '\n';

	for (const auto& header : headers)
	{
		for (const auto& header_value : header.second)
			out << "HEADER " << header.first << " " << header_value << '\n';
	}

	for (const auto& cookie : cookies)
	{
		if (!cookie.domain.empty())
		{
			const auto& domain = cookie.domain;
			const std::string default_path = "/";
			const auto& path = cookie.path.empty() ? default_path : cookie.path;
			out << "COOKIE_PATH_DOMAIN " << cookie.name << " " << path << " " << domain << " " << cookie.value << '\n';
		}
		else if (!cookie.path.empty())
		{
			out << "COOKIE_PATH " << cookie.name << " " << cookie.path << " " << cookie.value << '\n';
		}
		else
		{
			out << "COOKIE " << cookie.name << " " << cookie.value << '\n';
		}
	}

	out << '\n';
	out.flush();
}

void simple_cgi_response_string::send(std::ostream& out) const
{
	simple_cgi_response::send(out);
	out << body_string;
}

void simple_cgi_response_stream::send(std::ostream& out) const
{
	simple_cgi_response::send(out);
	body_stream(out);
}

simple_cgi_request simple_cgi_parse_request(std::istream& in)
{
	simple_cgi_request request;
	std::string line;
	while (std::getline(in, line) && !line.empty())
	{
		std::istringstream is(line);
		std::string tok;
		is >> tok;

		if (tok == "REQ_ID")
		{
			is >> request.req_id;
		}
		else if (tok == "MTD")
		{
			is >> request.method;
		}
		else if (tok == "ABS_PATH")
		{
			is >> request.abs_path;
		}
		else if (tok == "PATH")
		{
			is >> request.path;
		}
		else if (tok == "QUERY_S")
		{
			is >> request.query_string;
		}
		else if (tok == "QUERY")
		{
			std::string name;
			is >> name;
			std::string value(std::istreambuf_iterator<char>(is), {});
			request.query[name] = std::move(value);
		}
		else if (tok == "HEADER")
		{
			std::string name;
			is >> name;
			std::string value(std::istreambuf_iterator<char>(is), {});
			request.headers[name].emplace_back(std::move(value));
		}
	}

	request.body_stream = &in;
	return request;
}

