#include <simplecgi.h>

#include <sstream>
#include <utility>

void simple_cgi_main()
{
	simple_cgi_request request = simple_cgi_parse_request(std::cin);
	simple_cgi_response response;
	simple_cgi_handle(request, response);
	response.send(std::cout);
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

