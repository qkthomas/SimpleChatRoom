//============================================================================
// Name        : Server.cpp
// Author      : Lin Chen
// Version     :
// Copyright   : Your copyright notice
// Description : SimpleChatRoom in C++, Ansi-style
//============================================================================

#include <iostream>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/epoll.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <fcntl.h>
#include <unistd.h>
#include <stdio.h>
#include <errno.h>
#include <string>
#include <codecvt>
#include <locale>
#include <vector>
#include <cstring>
#include <netdb.h>

using namespace std;

#define MAX_EVENTS 500
#define BACK_LOG 32
#define INCOMING_BUF 512
#define HEADER_LENGTH 1

class ChatMessageHeader {
private:
	uint8_t m_message_length;
public:
	explicit ChatMessageHeader(int len) {
		m_message_length = len;
	}

	uint8_t GetLength(void) const {
		return m_message_length;
	}
};

class ChatServer {
private:
	static const string port;
private:
	int m_epollfd;
	int m_listenfd;
	struct epoll_event* m_events;
	vector<int> m_connected_socket_fds;
public:
	ChatServer () {
		m_epollfd = epoll_create1(0);
		m_events = new epoll_event[MAX_EVENTS];
		//listening socket's create, bind and listen
		struct addrinfo *result = new addrinfo();
		struct addrinfo hints;
		memset(&hints, 0, sizeof(hints));
		hints.ai_family = AF_INET;
		hints.ai_socktype = SOCK_STREAM;
		hints.ai_protocol = IPPROTO_TCP;
		hints.ai_flags = AI_PASSIVE;
		int ret = getaddrinfo(NULL, port.c_str(), &hints, &result);
		if (0 != ret) {
			cout << "getaddrinfo failed." << endl;
			abort();
		}
		m_listenfd = socket(result->ai_family, result->ai_socktype, result->ai_protocol);
		fcntl(m_listenfd, F_SETFL, O_NONBLOCK); // set non-blocking
		cout << "server listen fd = " << m_listenfd << ", listening port: " << port << endl;
		ret = bind(m_listenfd, result->ai_addr, (int)result->ai_addrlen);
		if (0 != ret) {
			cout << "bind failed." << endl;
			abort();
		}
		ret = listen(m_listenfd, BACK_LOG);
		if (0 != ret) {
			cout << "listen failed." << endl;
			abort();
		}
	}
	~ChatServer () {
		for (auto i : m_connected_socket_fds) {
			close(i);
		}
		close(m_listenfd);
		close(m_epollfd);
		delete[] m_events;
	}

	wstring s2ws(const std::string& str)
	{
	    using convert_typeX = std::codecvt_utf8<wchar_t>;
	    std::wstring_convert<convert_typeX, wchar_t> converterX;

	    return converterX.from_bytes(str);
	}

	string ws2s(const std::wstring& wstr)
	{
	    using convert_typeX = std::codecvt_utf8<wchar_t>;
	    std::wstring_convert<convert_typeX, wchar_t> converterX;

	    return converterX.to_bytes(wstr);
	}

	void Run() {
		//register listening socket to epoll
		struct epoll_event listen_event;
		listen_event.data.fd = m_listenfd;
		listen_event.events = EPOLLIN; // or EPOLLIN | EPOLLET for performance
		int ret = epoll_ctl(m_epollfd, EPOLL_CTL_ADD, m_listenfd, &listen_event);
		if (0 != ret) {
			cout << "Add listenfd epoll failed." << endl;
			return;
		}
		//event loop
		while (true) {
			cout << "Start epoll_wait." << endl;
			int event_num = epoll_wait(m_epollfd, m_events, MAX_EVENTS, -1);
			cout << "Done epoll_wait." << endl;
			for (int i = 0; i < event_num; i++) {
				if((m_events[i].events & EPOLLERR)||
					(m_events[i].events & EPOLLHUP)||
					(!(m_events[i].events & EPOLLIN))) {
					/* An error has occured on this fd, or the socket is not
					ready for reading (why were we notified then?) */
					cout << "epoll error" << endl;
					close(m_events[i].data.fd);
					continue;
				} else if (m_listenfd == m_events[i].data.fd) {
					//there is one or more incoming connection
					//try to accpet all of them until the listening queue is empty
					while (true) {
						struct sockaddr in_addr;
						socklen_t in_len;
						int incomingfd = accept(m_listenfd, &in_addr, &in_len);
						if (0 != incomingfd) {
							if ((errno== EAGAIN)|| (errno== EWOULDBLOCK)) {
								//all incoming connections have been processed
								break;
							} else {
								cout << "accept incoming, error" ;
								cout << "errno = " << errno << endl;
								break;
							}
						} else {
							//set incoming socket fd non-blocking
							if(fcntl(incomingfd, F_SETFL, O_NONBLOCK) < 0) {
								cout << "set incoming socket fd non-blocking failed." << endl;
								break;
							} else {
								cout << "an incoming socket." << endl;
							}
							struct epoll_event event;
							event.data.fd = incomingfd;
							event.events = EPOLLIN; //or EPOLLIN | EPOLLET
							//register incomingfd to epollfd
							ret = epoll_ctl(m_epollfd, EPOLL_CTL_ADD, incomingfd, &event);
							if (0 != ret) {
								cout << "adding incoming socket fd to epoll failed." << endl;
								return;
							}
							m_connected_socket_fds.push_back(incomingfd);
						}
					}
				} else {
					bool done = false;
					while (true) {
						//to read incoming data
						ssize_t header_count;
						char header_buf[HEADER_LENGTH];
						header_count = read(m_events[i].data.fd, header_buf, HEADER_LENGTH);
						if (-1 == header_count) {
							/* If errno == EAGAIN, that means we have read all
							data. So go back to the main loop. */
							if(errno!= EAGAIN) {
								done = true;
							}
							break;
						} else if (0 == header_count) {
							//end of file
							done = true;
							break;
						} else {
							uint8_t remaining_message_len = (uint8_t)header_buf[0];
							char buf[INCOMING_BUF];
							while (remaining_message_len > 0) {
								ssize_t message_count;
								message_count = read(m_events[i].data.fd, buf, remaining_message_len);
								remaining_message_len -= message_count;
							}
							string str(buf, (uint8_t)header_buf[0]);
							wstring wstr = s2ws(str);
							cout << "-----------------Incoming Message-----------------" << endl;
							wcout << wstr << endl;
						}
					}
				}
			}
		}
	}
};

const string ChatServer::port = "14939";

int main(int argc,  char** argv) {
	ChatServer server;
	server.Run();
	return 0;
}
