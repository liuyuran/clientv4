@echo off
rd /s /q "generated"
flatc -I .\proto --csharp .\proto\block-define.fbs