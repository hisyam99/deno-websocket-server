// File: /WebSocketSecure_Server.ts
import { WebSocketServer, WebSocketUser, Room } from "@dgpg/chatosaurus";
import { Context, Next } from "@dgpg/chatosaurus";
import { ulid } from "jsr:@std/ulid";

// Membuka database Deno KV
const kv = await Deno.openKv();

// Interface untuk data pengguna
interface UserData {
  id: string;
  username: string;
  authToken: string;
}

// Interface untuk data ruang
interface RoomData {
  id: string;
  name: string;
}

// Interface untuk data pesan
interface MessageData {
  id: string;
  roomId: string;
  userId: string;
  content: string;
  timestamp: number;
}

// Middleware untuk logging setiap event yang terjadi
const logMiddleware = async (context: Context, next: Next) => {
  console.log(
    `[${new Date().toISOString()}] Event: ${context.evt}, User: ${
      context.ws.id
    }, Username: ${(context.ws as any).username || "Unknown"}`
  );
  await next();
};

// Middleware untuk autentikasi pengguna menggunakan Deno KV
const authMiddleware = async (context: Context, next: Next) => {
  const [eventData] = context.data as { authToken?: string }[];
  if (!eventData?.authToken) {
    context.ws.invoke("error", "Authentication required.");
    return;
  }

  // Memeriksa token autentikasi pengguna di Deno KV
  const userResult = await kv.get<UserData>(["users", eventData.authToken]);
  if (!userResult.value) {
    context.ws.invoke("error", "Invalid authentication token.");
    return;
  }

  // Menyimpan data pengguna di objek WebSocketUser
  (context.ws as any).userData = userResult.value;

  console.log(`User ${context.ws.id} authenticated successfully.`);
  await next();
};

// Membuat instance WebSocketServer dengan konfigurasi tertentu
const server = new WebSocketServer({
  hostname: "0.0.0.0",
  port: Number(Deno.env.get("PORT") || 8080),
});

server.use(logMiddleware);
server.use(authMiddleware);

// Menangani event saat pengguna terhubung ke server
server.on("onConnect", async (user: WebSocketUser) => {
  console.log(`User connected: ${user.id}`);

  // Menghasilkan ID pengguna baru
  const userId = ulid();

  // Menyimpan koneksi pengguna di Deno KV
  await kv.set(["connections", userId], {
    id: userId,
    socketId: user.id,
    timestamp: Date.now(),
  });

  // Mengirim pesan selamat datang ke pengguna yang baru terhubung
  user.invoke("welcome", { id: userId, message: "Welcome to the server!" });
});

// Menangani event saat pengguna bergabung ke ruang
server.on(
  "join",
  async (user: WebSocketUser, data: { roomId: string; authToken: string }) => {
    // Mengautentikasi pengguna (dilakukan oleh authMiddleware)
    const userData = (user as any).userData as UserData;

    // Memeriksa apakah ruang ada di Deno KV
    const roomResult = await kv.get<RoomData>(["rooms", data.roomId]);
    if (!roomResult.value) {
      user.invoke("error", `Room with ID ${data.roomId} not found.`);
      return;
    }

    // Bergabung ke ruang
    user.join(data.roomId);

    // Menyimpan keanggotaan ruang pengguna di Deno KV
    await kv
      .atomic()
      .set(["userRooms", userData.id, data.roomId], true)
      .set(["roomMembers", data.roomId, userData.id], true)
      .commit();

    console.log(
      `User ${userData.id} (${userData.username}) joined room ${data.roomId}`
    );
  }
);

// Menangani event saat pengguna meninggalkan ruang
server.on("leave", async (user: WebSocketUser, roomId: string) => {
  const userData = (user as any).userData as UserData;

  // Meninggalkan ruang
  server.roomManager.leave(roomId, user);

  // Menghapus keanggotaan ruang pengguna dari Deno KV
  await kv
    .atomic()
    .delete(["userRooms", userData.id, roomId])
    .delete(["roomMembers", roomId, userData.id])
    .commit();

  console.log(`User ${userData.id} (${userData.username}) left room ${roomId}`);
});

// Menangani event saat pengguna mengirim pesan pribadi
server.on(
  "privateMessage",
  async (
    user: WebSocketUser,
    data: { targetId: string; message: string; authToken: string }
  ) => {
    const userData = (user as any).userData as UserData;

    // Mencari pengguna target di Deno KV
    const targetResult = await kv.get<UserData>(["users", data.targetId]);
    if (!targetResult.value) {
      user.invoke("error", `User with ID ${data.targetId} not found.`);
      return;
    }

    // Mencari koneksi pengguna target
    const targetConnectionResult = await kv.get<{
      id: string;
      socketId: string;
    }>(["connections", data.targetId]);
    if (!targetConnectionResult.value) {
      user.invoke("error", `User ${data.targetId} is not connected.`);
      return;
    }

    // Mengirim pesan pribadi ke pengguna target
    const targetUser = server.clients.find(
      (client) => client.id === targetConnectionResult.value.socketId
    );
    if (targetUser) {
      targetUser.invoke("privateMessage", {
        from: userData.id,
        username: userData.username,
        message: data.message,
      });
    } else {
      user.invoke("error", `User ${data.targetId} is not currently connected.`);
    }
  }
);

// Menangani event saat pengguna mengirim pesan broadcast
server.on(
  "broadcast",
  async (
    user: WebSocketUser,
    data: { message: string; roomId: string; authToken: string }
  ) => {
    const userData = (user as any).userData as UserData;

    // Memeriksa apakah ruang ada
    const roomResult = await kv.get<RoomData>(["rooms", data.roomId]);
    if (!roomResult.value) {
      user.invoke("error", `Room with ID ${data.roomId} not found.`);
      return;
    }

    // Mendapatkan ruang
    const room = server.roomManager.getRoom(data.roomId);
    if (!room) {
      user.invoke("error", `Room with ID ${data.roomId} not found.`);
      return;
    }

    // Menghasilkan ID pesan unik
    const messageId = ulid();

    // Menyimpan pesan di Deno KV
    await kv.set(["messages", data.roomId, messageId], {
      id: messageId,
      roomId: data.roomId,
      userId: userData.id,
      content: data.message,
      timestamp: Date.now(),
    });

    // Mengirim pesan broadcast ke ruang
    room.invoke("message", {
      id: messageId,
      from: userData.id,
      username: userData.username,
      message: data.message,
      timestamp: Date.now(),
    });
  }
);

// Menangani event kesalahan yang terjadi di server
server.on("error", (err) => {
  console.error(`[Server Error]: ${err.message}`);
  console.error(`Stack trace: ${err.stack}`);
});

// Memulai server WebSocket
server.start();

// Fungsi bantu untuk membuat ruang baru
async function createRoom(name: string): Promise<string> {
  const roomId = ulid();
  await kv.set(["rooms", roomId], { id: roomId, name });
  return roomId;
}

// Fungsi bantu untuk mencantumkan ruang
async function listRooms(): Promise<RoomData[]> {
  const rooms: RoomData[] = [];
  const iter = kv.list<RoomData>({ prefix: ["rooms"] });
  for await (const entry of iter) {
    rooms.push(entry.value);
  }
  return rooms;
}

// Fungsi bantu untuk mencantumkan pesan di ruang
async function listMessages(roomId: string): Promise<MessageData[]> {
  const messages: MessageData[] = [];
  const iter = kv.list<MessageData>({ prefix: ["messages", roomId] });
  for await (const entry of iter) {
    messages.push(entry.value);
  }
  return messages.sort((a, b) => a.timestamp - b.timestamp);
}

// Menutup database Deno KV saat server berhenti
async function closeServer() {
  await kv.close();
  console.log("Deno KV database closed.");
}

// Menangani sinyal SIGINT untuk menutup server secara elegan
Deno.addSignalListener("SIGINT", async () => {
  console.log("Shutting down server...");
  await server.stop();
  await closeServer();
  Deno.exit(0);
});
