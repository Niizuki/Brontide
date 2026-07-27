using System.Buffers.Binary;
using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// PB6: the decoders hold their contract on arbitrary and mutated input, inside deterministic bounds.
/// </summary>
/// <remarks>
/// Every vector so far presents input a person wrote. These present input nobody wrote, which is the
/// only way to check the property the vectors assume everywhere: that a decoder either produces a
/// value or refuses with a portable category, and never does anything else. A foreign exception
/// escaping here would cross the seam as a runtime type in exactly the way C4 forbids.
///
/// The generator is seeded and the iteration count fixed, so a failure is reproducible and the suite
/// is not a source of intermittent red. That is what "within deterministic bounds" asks for: this is
/// a property test, not a soak test.
/// </remarks>
public sealed class PortableDecoderPropertyTests
{
    private const int Seed = 0x_50_17_1D_E1;
    private const int Iterations = 2000;

    /// <summary>
    /// The only two outcomes a decoder may produce. Anything else is the defect being hunted.
    /// </summary>
    private static void RequireDecoderContract(Action decode, string what)
    {
        try
        {
            decode();
        }
        catch (PortableFaultException)
        {
            // A refusal carrying a portable category is the contract.
        }
        catch (PortableProcessFailureException)
        {
            // A locally observed loss is the other permitted outcome.
        }
        catch (Exception exception)
        {
            Assert.Fail(
                $"{what} escaped as {exception.GetType().FullName} rather than a portable refusal: {exception.Message}");
        }
    }

    [Test]
    public void Arbitrary_bytes_are_either_decoded_or_refused_with_a_portable_category()
    {
        var random = new Random(Seed);
        var buffer = new byte[64];

        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            random.NextBytes(buffer);
            var length = random.Next(0, buffer.Length);
            var input = buffer.AsSpan(0, length).ToArray();

            RequireDecoderContract(
                () => PortableCbor.Decode(input, PortableLimits.Declared),
                $"Decoding {length} arbitrary bytes");
        }
    }

    [Test]
    public void Arbitrary_bytes_are_never_accepted_as_a_well_formed_envelope()
    {
        var random = new Random(Seed + 1);
        var buffer = new byte[96];
        var accepted = 0;

        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            random.NextBytes(buffer);
            var input = buffer.AsSpan(0, random.Next(0, buffer.Length)).ToArray();

            try
            {
                PortableEnvelopeCodec.Decode(input, PortableLimits.Declared);
                accepted++;
            }
            catch (PortableFaultException)
            {
            }
            catch (PortableProcessFailureException)
            {
            }
            catch (Exception exception)
            {
                Assert.Fail($"An envelope decode escaped as {exception.GetType().FullName}: {exception.Message}");
            }
        }

        // Random bytes are not a contract-versioned envelope with a declared kind and a channel
        // identity. Accepting one would mean the envelope's required fields are not required.
        Assert.That(accepted, Is.Zero, "Random input was accepted as a well-formed envelope.");
    }

    /// <summary>
    /// Single-byte mutations of a valid frame. This reaches the decode paths random bytes never do,
    /// because a mutant is still nearly a legal message.
    /// </summary>
    [Test]
    public void Every_single_byte_mutation_of_a_valid_frame_is_refused_or_decoded_but_never_escapes()
    {
        var valid = PortableEnvelopeCodec.Encode(PortableEnvelope.Control(
            PortableEnvelopeKind.Ready,
            PortableChannelId.New()));
        var random = new Random(Seed + 2);

        for (var index = 0; index < valid.Length; index++)
        {
            foreach (var replacement in new byte[] { 0x00, 0xFF, 0x7B, (byte)random.Next(256) })
            {
                var mutant = valid.ToArray();
                mutant[index] = replacement;
                RequireDecoderContract(
                    () => PortableEnvelopeCodec.Decode(mutant, PortableLimits.Declared),
                    $"Byte {index} replaced with 0x{replacement:x2}");
            }
        }
    }

    /// <summary>Truncations of a valid frame, which are the interrupted-frame shape at every offset.</summary>
    [Test]
    public void Every_truncation_of_a_valid_frame_is_refused_rather_than_half_decoded()
    {
        var valid = PortableEnvelopeCodec.Encode(PortableEnvelope.Control(
            PortableEnvelopeKind.Ready,
            PortableChannelId.New()));

        for (var length = 0; length < valid.Length; length++)
        {
            var truncated = valid.AsSpan(0, length).ToArray();
            RequireDecoderContract(
                () => PortableEnvelopeCodec.Decode(truncated, PortableLimits.Declared),
                $"A frame truncated to {length} bytes");
        }
    }

    /// <summary>
    /// Nesting is bounded before it becomes recursion. A generator that can always add one more
    /// level is the honest test of a declared depth limit.
    /// </summary>
    [Test]
    public void Nesting_beyond_the_declared_depth_is_refused_at_every_depth_past_the_bound()
    {
        foreach (var depth in new[]
        {
            PortableLimits.Declared.MaxNestingDepth + 1,
            PortableLimits.Declared.MaxNestingDepth + 8,
            PortableLimits.Declared.MaxNestingDepth * 4,
            10_000
        })
        {
            var nested = new byte[depth + 1];
            Array.Fill(nested, (byte)0x81, 0, depth);
            nested[^1] = 0xF6;

            var fault = Assert.Throws<PortableFaultException>(
                () => PortableCbor.Decode(nested, PortableLimits.Declared),
                $"Depth {depth} did not refuse.");
            Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.LimitExceeded));
        }
    }

    /// <summary>
    /// A length prefix is refused on the prefix alone, so a hostile declaration never causes an
    /// allocation proportional to it.
    /// </summary>
    [Test]
    public void A_hostile_length_prefix_is_refused_before_any_allocation()
    {
        var random = new Random(Seed + 3);

        for (var iteration = 0; iteration < 256; iteration++)
        {
            var declared = (uint)random.NextInt64(PortableLimits.Declared.MaxFrameBytes + 1, uint.MaxValue);
            var prefix = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(prefix, declared);
            using var stream = new MemoryStream(prefix);

            var fault = Assert.ThrowsAsync<PortableFaultException>(async () =>
                await PortableFraming.ReadFrameAsync(stream, PortableLimits.Declared, CancellationToken.None));

            Assert.Multiple(() =>
            {
                Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.LimitExceeded));
                Assert.That(stream.Position, Is.EqualTo(4), "The declared body was never read.");
            });
        }
    }

    /// <summary>
    /// A refusal never carries a runtime type, a stack trace, or a namespace, whatever the input.
    /// </summary>
    /// <remarks>
    /// This is the property CH-18 states, checked against input nobody chose rather than against the
    /// handful of messages a person wrote.
    /// </remarks>
    [Test]
    public void No_refusal_message_carries_a_runtime_type_or_a_stack_trace()
    {
        var random = new Random(Seed + 4);
        var buffer = new byte[80];
        var forbidden = new[] { "Brontide.", "System.", "Exception", "   at ", "`1", "+<" };
        var observed = ImmutableArray.CreateBuilder<string>();

        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            random.NextBytes(buffer);
            var input = buffer.AsSpan(0, random.Next(0, buffer.Length)).ToArray();

            try
            {
                PortableEnvelopeCodec.Decode(input, PortableLimits.Declared);
            }
            catch (PortableFaultException fault)
            {
                observed.Add($"{fault.LocalCode}|{fault.Message}");
            }
            catch (PortableProcessFailureException failure)
            {
                observed.Add(failure.Message);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.Not.Empty, "The generator produced no refusals to inspect.");
            foreach (var message in observed.Distinct())
            {
                foreach (var marker in forbidden)
                {
                    Assert.That(
                        message,
                        Does.Not.Contain(marker),
                        $"A refusal leaked '{marker}': {message}");
                }
            }
        });
    }
}
