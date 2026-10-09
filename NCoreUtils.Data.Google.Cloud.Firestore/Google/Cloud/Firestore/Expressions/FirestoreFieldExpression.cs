using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

public abstract class FirestoreFieldExpression : Expression, IExtensionExpression
{
    // private static readonly MethodInfo _mGetValue;

    // private static readonly MethodInfo _mContainsField;

    private static readonly PropertyInfo _pDocumentSnapshotId;

    [SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Wooulld be very noisy")]
    static FirestoreFieldExpression()
    {
        // Expression<Func<DocumentSnapshot, FieldPath, Value>> e0 = (doc, name) => doc.GetValue<Value>(name);
        // _mGetValue = ((MethodCallExpression)e0.Body).Method;
        // Expression<Func<DocumentSnapshot, FieldPath, bool>> e1 = (doc, name) => doc.ContainsField(name);
        // _mContainsField = ((MethodCallExpression)e1.Body).Method;
        Expression<Func<DocumentSnapshot, string>> e2 = doc => doc.Id;
        _pDocumentSnapshotId = (PropertyInfo)((MemberExpression)e2.Body).Member;
    }

    protected static Value CreateNullValueValue() => new() { NullValue = default };

    internal static Value GetValueOrCreateNullValue(Dictionary<string, Value> map, string path)
        => map.TryGetValue(path, out var value)
            ? value
            : CreateNullValueValue();

    public override bool CanReduce => true;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type { get; }

    // FIXME: consider using single string as there is no use case where this field holds more than a single
    // value... At least right now...
    /// <summary>
    /// Raw path of the field, <see langword="null" /> only if the field refers to the ID (aka primary key) of the
    /// entity.
    /// </summary>
    public ImmutableList<string>? RawPath { get; }

    public FieldPath Path { get; }

    public Expression Instance { get; }

    public FirestoreConverter Converter { get; }

    protected FirestoreFieldExpression(FirestoreConverter converter, Expression instance, ImmutableList<string>? rawPath, FieldPath path, Type type)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Converter = converter ?? throw new ArgumentNullException(nameof(converter));
        RawPath = rawPath;
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        if (instance.Type != typeof(DocumentSnapshot))
        {
            throw new InvalidOperationException("Firestore field expression can only be created for document snapshots.");
        }
    }

    protected abstract Expression ReduceToNonExtension();

    internal abstract Expression ToReadBackExpression(Expression valueDictionary, Expression id);

    public override Expression Reduce()
    {
        if (Path.Equals(FieldPath.DocumentId))
        {
            return Property(Instance, _pDocumentSnapshotId);
        }
        return ReduceToNonExtension();
    }

    public override string ToString()
        => $"{Instance}[{Path}]";

    public abstract Expression AcceptNoReduce(ExpressionVisitor visitor);
}

public class FirestoreFieldExpression<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : FirestoreFieldExpression
{
    private static readonly Expression<Func<DocumentSnapshot, FirestoreConverter, FieldPath, T>> _template
        = (doc, converter, path) => converter.ConvertFromValue<T>(
            doc.ContainsField(path)
                ? doc.GetValue<Value>(path)
                : CreateNullValueValue()
        );

    private static readonly Expression<Func<Dictionary<string, Value>, FirestoreConverter, string, T>> _readbackTemplate
        = (map, converter, path) => converter.ConvertFromValue<T>(
            GetValueOrCreateNullValue(map, path)
        );

    private FirestoreFieldExpression(FirestoreConverter converter, Expression instance, ImmutableList<string>? rawPath, FieldPath path)
        : base(converter, instance, rawPath, path, typeof(T))
    { }

    public FirestoreFieldExpression(FirestoreConverter converter, Expression instance, ImmutableList<string> rawPath)
        : this(
            converter,
            instance,
            rawPath ?? throw new ArgumentNullException(nameof(rawPath), "For special paths use overloaded constructor."),
            new FieldPath([.. rawPath]))
    { }

    public FirestoreFieldExpression(FirestoreConverter converter, Expression instance, FieldPath specialPath)
        : this(converter, instance, default, specialPath)
    { }

    protected override Expression ReduceToNonExtension()
    {
        return _template.Body
            .SubstituteParameter(_template.Parameters[0], Instance)
            .SubstituteParameter(_template.Parameters[1], Constant(Converter))
            .SubstituteParameter(_template.Parameters[2], Constant(Path));
    }

    internal override Expression ToReadBackExpression(Expression valueDictionary, Expression id)
    {
        if (RawPath is null)
        {
            if (Path.Equals(FieldPath.DocumentId))
            {
                return id;
            }
            throw new InvalidOperationException($"Cannot convert field expression without raw path to readback expression.");
        }
        if (RawPath is not [var singlePathEntry])
        {
            throw new InvalidOperationException($"Cannot convert field expression for {string.Join('.', RawPath)} to readback expression.");
        }
        return _readbackTemplate.Body
            .SubstituteParameter(_readbackTemplate.Parameters[0], valueDictionary)
            .SubstituteParameter(_readbackTemplate.Parameters[1], Constant(Converter))
            .SubstituteParameter(_readbackTemplate.Parameters[2], Constant(singlePathEntry));
    }

    public override Expression AcceptNoReduce(ExpressionVisitor visitor)
    {
        Preconditions.ThrowIfNull(visitor);
        var newInstance = visitor.Visit(Instance);
        return ReferenceEquals(newInstance, Instance)
            ? this
            : new FirestoreFieldExpression<T>(Converter, newInstance, RawPath, Path);
    }
}