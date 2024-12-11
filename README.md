# Deno WebSocket Server

Proyek ini adalah implementasi WebSocket dengan menggunakan Deno, sebuah runtime JavaScript yang modern dan aman. Proyek ini terdiri dari dua bagian utama: server WebSocket dan client WebSocket.

## Server WebSocket

Server WebSocket dibuat menggunakan library [**@dgpg/chatosaurus**](https://jsr.io/@dgpg/chatosaurus). Server ini memiliki beberapa fitur, termasuk:

*   Autentikasi pengguna dengan token
*   Pengiriman pesan broadcast ke semua pengguna
*   Pengiriman pesan private ke pengguna tertentu
*   Pengguna dapat bergabung dengan room dan meninggalkan room

### Fitur Server

*   **Autentikasi**: Pengguna harus memiliki token autentikasi yang valid untuk dapat terhubung ke server.
*   **Pesan Broadcast**: Pengguna dapat mengirimkan pesan broadcast ke semua pengguna yang terhubung ke server.
*   **Pesan Private**: Pengguna dapat mengirimkan pesan private ke pengguna lain yang terhubung ke server.
*   **Room**: Pengguna dapat bergabung dengan room dan meninggalkan room.

### Cara Menjalankan Server

```bash
deno run --allow-net WebSocket_Server.ts
```

## Client WebSocket

Client WebSocket dibuat menggunakan library [**@dgpg/chatosaurus**](https://jsr.io/@dgpg/chatosaurus). Client ini memiliki beberapa fitur, termasuk:

*   Menghubungkan ke server WebSocket
*   Mengirimkan perintah join ke server
*   Mengirimkan perintah broadcast ke server
*   Mengirimkan perintah private message ke server

### Fitur Client

*   **Menghubungkan ke Server**: Client dapat menghubungkan ke server WebSocket.
*   **Mengirimkan Perintah**: Client dapat mengirimkan perintah join, broadcast, dan private message ke server.

### Cara Menjalankan Client



```bash
deno run --allow-net WebSocket_Client.ts
```

## Deno.json

File `deno.json` digunakan untuk mengkonfigurasi import library yang digunakan oleh proyek. Pada file ini, kita mengimport library [**@dgpg/chatosaurus**](https://jsr.io/@dgpg/chatosaurus).