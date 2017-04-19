rmdir /S /Q bin
mkdir bin
copy lib bin
copy app.exe.config bin
csc /out:bin\app.exe @ref.rsp /recurse:src\*.cs