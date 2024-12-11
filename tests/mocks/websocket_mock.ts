// tests/mocks/websocket_mock.ts
import { WebSocketUser, WebSocketServer, Room } from "@dgpg/chatosaurus";
import { assertEquals, assertExists } from "jsr:@std/assert";

export class MockWebSocketUser implements WebSocketUser {
  id: string;
  private _invokedEvents: { event: string; data: any }[] = [];

  constructor(id: string) {
    this.id = id;
  }
  rooms!: string[];
  leave(roomId: string): void {
    throw new Error("Method not implemented.");
  }
  leaveAll(): void {
    throw new Error("Method not implemented.");
  }
  room(roomId: string): Room | undefined {
    throw new Error("Method not implemented.");
  }
  to(clientId: string): void {
    throw new Error("Method not implemented.");
  }
  broadcast(evt: string, ...args: unknown[]): void {
    throw new Error("Method not implemented.");
  }
  sendJSON(data: unknown): void {
    throw new Error("Method not implemented.");
  }
  binaryType!: BinaryType;
  bufferedAmount!: number;
  extensions!: string;
  onclose!: ((this: WebSocket, ev: CloseEvent) => any) | null;
  onerror!: ((this: WebSocket, ev: Event | ErrorEvent) => any) | null;
  onmessage!: ((this: WebSocket, ev: MessageEvent) => any) | null;
  onopen!: ((this: WebSocket, ev: Event) => any) | null;
  protocol!: string;
  readyState!: number;
  url!: string;
  close(code?: number, reason?: string): void {
    throw new Error("Method not implemented.");
  }
  send(data: string | ArrayBufferLike | Blob | ArrayBufferView): void {
    throw new Error("Method not implemented.");
  }
  CLOSED!: number;
  CLOSING!: number;
  CONNECTING!: number;
  OPEN!: number;
  addEventListener(type: unknown, listener: unknown, options?: unknown): void {
    throw new Error("Method not implemented.");
  }
  removeEventListener(type: unknown, listener: unknown, options?: unknown): void {
    throw new Error("Method not implemented.");
  }
  dispatchEvent(event: Event): boolean {
    throw new Error("Method not implemented.");
  }

  invoke(event: string, data: any) {
    this._invokedEvents.push({ event, data });
  }

  join(roomId: string) {
    // Mock implementation
  }

  getInvokedEvents() {
    return this._invokedEvents;
  }

  clearInvokedEvents() {
    this._invokedEvents = [];
  }
}

export class MockWebSocketServer extends WebSocketServer {
  private _clients: MockWebSocketUser[] = [];
  public override get clients(): MockWebSocketUser[] {
    return this._clients;
  }
  public override set clients(value: MockWebSocketUser[]) {
    this._clients = value;
  }
  
  createUser(id: string): MockWebSocketUser {
    const user = new MockWebSocketUser(id);
    this.clients.push(user);
    return user;
  }
}