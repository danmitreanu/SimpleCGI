#include <simplecgi.h>

#include <sstream>
#include <utility>

void simple_cgi_main()
{
	simple_cgi_request request = simple_cgi_parse_request(std::cin);
	simple_cgi_response response;
	response.req_id = request.req_id;

	simple_cgi_handle(request, response);
	response.send(std::cout);
	std::cout.flush();
}
