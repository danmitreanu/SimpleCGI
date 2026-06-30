#include <simplecgi.h>

SIMPLE_CGI_IMPLEMENT()

void simple_cgi_handle(const simple_cgi_request& request, simple_cgi_response& response)
{
	response.status_code = 200;
	response.content_type = "text/html";

	response.body_writer = [](std::ostream& out)
		{
			out << "<h1>Hello, World!</h1>";
		};
}

