#!/bin/bash
curl -si http://127.0.0.1:20128/api/auth/login \
  -X POST \
  -H 'Content-Type: application/json' \
  -H 'Host: localhost:20128' \
  -d '{"password":"123456"}' | head -40
