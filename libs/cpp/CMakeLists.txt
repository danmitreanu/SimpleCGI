cmake_minimum_required(VERSION 3.50)
project(SimpleCGI-cpp-exe)

set(CGI_EXE_ROOT ${CMAKE_CURRENT_SOURCE_DIR})
file(GLOB CGI_EXE_SRC_FILES "${CGI_EXE_ROOT}/src/*.cpp")
file(GLOB CGI_EXE_INC_FILES "${CGI_EXE_ROOT}/include/*.h")

add_library(cgi_exe STATIC ${CGI_EXE_INC_FILES} ${CGI_EXE_SRC_FILES})
target_include_directories(cgi_exe PUBLIC "${CGI_EXE_ROOT}/include")

add_executable(cgi_exe_test "${CGI_EXE_ROOT}/test/test1.cpp")
target_link_libraries(cgi_exe_test PUBLIC cgi_exe)
