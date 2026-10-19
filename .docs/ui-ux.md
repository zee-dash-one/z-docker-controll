# UI / UX — Z-Lab 2.0

**Authority:** [`docs/SPEC-ZLAB.md`](../docs/SPEC-ZLAB.md)  
**UI stack:** .NET 8 WinForms · PT-BR · icon `z-lab.ico`

The panel is a lab bench for one server, not a catalog of disabled templates and not a backup console.

## 1. Hierarchy

```text
┌─ Z-Lab ─────────────────────────────────────────────┐
│ Sem sessão / usuário@192.168.2.37                   │
│ [Liga] [Desligar] [Reiniciar] [Entrar] [Encerrar sessão] [Sair]
│ Tempo no ar / Carga / Memória / Disco / / Docker / /data
│ ┌ card ┐ ┌ card ┐ ┌ card ┐  (seis serviços)          │
│ Saída                                                 │
└───────────────────────────────────────────────────────┘
```

1. Session and power (Liga stays available when the server is down).
2. Host readings.
3. One card per service: name, status, LAN URL or HTTPS label, then Subir, Parar, Criar, Atualizar, Logs, Uso, and Abrir when a LAN URL exists.
4. Command output at the bottom.

Closing the window hides it. Sair is explicit and says the containers keep running.

## 2. Cards

| Status | Verbs enabled (when a session is open) |
|--------|----------------------------------------|
| Ausente | Criar, Atualizar |
| Parado | Subir, Atualizar, Logs |
| No ar | Parar, Atualizar, Logs, Uso, Abrir |
| Sem sessão | none of the above; Liga and Entrar stay available |

Atualizar, Desligar, and Reiniciar ask before sending. Subir and Parar do not.

HTTPS names are plain text. Abrir is only the two LAN URLs.

## 3. Tone

PT-BR, short verbs, no sound pack. Tray tooltip summarizes the six statuses. Balloons only for caiu / subiu. Busy state disables mutating buttons until the remote command returns.

## 4. What left the panel

Backup folder, catalog of disabled templates, Expor LAN, Tunnel, hosts editor, engine mode `wsl docker`, and “parar tudo ao sair”.
