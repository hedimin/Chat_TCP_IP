# Chat TCP/IP MAUI

This is a primitive TCP/IP MAUI chat

# Working diagram:

Connection:

![Діаграма без назви drawio (3)](https://user-images.githubusercontent.com/112476754/211387853-69510c02-39e7-481c-bea2-84b712c3a3bb.png)

Handling connection:

![Діаграма без назви drawio (4)](https://user-images.githubusercontent.com/112476754/211396421-294f9df8-ce5b-4e71-a657-f5121f5d02f7.png)


# Server
The server is a conssole application. After start, it begins to listen for incoming connections:

![image](https://user-images.githubusercontent.com/112476754/211390432-2be82527-3942-4d72-ad09-ce6cbabe934a.png)

After connection is established, client's remoted endpoint is written:

![image](https://user-images.githubusercontent.com/112476754/211390808-7d02852f-45e2-4916-9700-99c01a42cde6.png)

Also server writes into console client's messages or requests:

![image](https://user-images.githubusercontent.com/112476754/211391766-6a59a84c-c212-4158-9217-5fbd6148235a.png)


# Client
The client is a MAUI app (windows app as example below):

![image](https://user-images.githubusercontent.com/112476754/211391138-41f8a60b-6db6-4a39-9a08-5445591a28f7.png)

It has two main text field, left one is for chat messages, the other is for log information. 

![image](https://user-images.githubusercontent.com/112476754/211391621-61d603b1-a8bc-4910-94b7-46b8f0b95939.png)

Also, there is an ability to set nicknames ('Send message' and 'change nikcname' buttons uses Binding to change color if relative text field is or not empty)

![image](https://user-images.githubusercontent.com/112476754/211392430-af662e36-17db-46f1-af04-4e8edf1603ea.png)

![image](https://user-images.githubusercontent.com/112476754/211392633-7e9347e2-de81-42df-a91a-245d7d1f4563.png)

After one client sets nickname, other clients will not see it's remote endpoint:

![image](https://user-images.githubusercontent.com/112476754/211392922-82ea809d-b763-4c0f-bd20-adbb296ce896.png)

# Details:
To make stream of messages, not stream of bytes, app uses System.IO.Pipelines. Each socket connection has two unidirectional streams of bytes. Also every message has prefix length that defines it's type (as example, keepalive messages that a used to deal with a half-opened state of connected socket). To de/serealize data app uses Messagepack.
