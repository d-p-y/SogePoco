using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace SogePoco.Impl.Tests.Compiler; 

public class InMemoryAssembly {
    private readonly MemoryStream _stream;
    private byte[]? _arr;
    private PortableExecutableReference? _ref;

    public InMemoryAssembly(MemoryStream stream) {
        _stream = stream;
    }
        
    public byte[] ToBytes() {
        if (_arr == null) {
            _stream.Seek(0, SeekOrigin.Begin);
            _arr = _stream.ToArray();
        }

        return _arr;
    }
        
    public Assembly ToAssembly() {
        if (_arr == null) {
            _stream.Seek(0, SeekOrigin.Begin);
            _arr = _stream.ToArray();
        }

        return Assembly.Load(_arr);
    }

    public MetadataReference ToMetadataReference() {
        if (_arr == null) {
            _stream.Seek(0, SeekOrigin.Begin);
            _arr = _stream.ToArray();
        }
            
        if (_ref == null) {
            _stream.Seek(0, SeekOrigin.Begin);
            _ref = MetadataReference.CreateFromStream(_stream);
        }

        return _ref;
    }
}