rmdir /S /Q bin
mkdir bin
copy lib bin
copy app.exe.config bin
csc /out:bin\app.exe /doc:doc.xml @ref.rsp /recurse:src\*.cs