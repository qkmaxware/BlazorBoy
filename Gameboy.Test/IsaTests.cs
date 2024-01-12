using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Test;

[TestClass]
public class IsaTests : GbTests {

    public void RunUntilEnd(Gameboy gb) {
        // Run the cart
        try {
            for (var i = 0; i < 8_000_000; i++) { // Randomly picked... really should have a calcuation to this. 
                gb.Dispatch();
            }
        } catch (Exception e) {
            throw new Exception($"Runtime failed at 0x{gb.CPU.Registry.pc():X4}", e);
        }
    }

    public void ValidateRomOutput(StringWriter output) {
        var results = output.ToString().Trim();
        if (!results.EndsWith("Passed")) {
            Assert.Fail("\"" + results + "\"");
        }
    }
    public void ValidateRomOutput(string header, StringWriter output) {
        var results = output.ToString().Trim();
        if (!results.StartsWith(header) || !results.EndsWith("Passed")) {
            Assert.Fail("\"" + results + "\"");
        }
    }

    class NullMemory : IMemory {
        

        public int ReadByte(int address) {
            throw new NotImplementedException();
        }

        public int ReadShort(int address) {
            throw new NotImplementedException();
        }

        public void Reset() {
            throw new NotImplementedException();
        }

        public void WriteByte(int address, int value) {
            throw new NotImplementedException();
        }

        public void WriteShort(int address, int value) {
            throw new NotImplementedException();
        }
    }

    [TestMethod]
    public void TestNoMissingInstructions() {
        var isa = ReadJson<OpcodesJsonModel>("opcodes");
        if (isa.unprefixed is null)
            Assert.Fail("Missing unprefiex ISA specifications");
        if (isa.cbprefixed is null) 
            Assert.Fail("Missing cbprefixed ISA specifications");
        if (isa.unprefixed.Count == 0)
            Assert.Fail("Failed to load opcode ISA specifications");
        if (isa.cbprefixed.Count == 0)
            Assert.Fail("Failed to load cb-prefixed opcode ISA specifications");

        var cpu = new Cpu(new NullMemory());
        var instructions = Enumerable.Range(0, 255).Select(opcode => { try { return cpu.FetchOperation(opcode); } catch { return null; }}).ToArray();
        Assert.AreEqual(255, instructions.Length);                              // Assert the number of instructions is good
        Assert.AreEqual(true, instructions.Where(op => op is not null).Any());  // Assert has at least one instruction
        var cbinstructions = Enumerable.Range(0, 255).Select(opcode => { try { return cpu.FetchCbPrefixedOperation(opcode); } catch { return null; }}).ToArray();
        Assert.AreEqual(255, cbinstructions.Length);                              // Assert the number of instructions is good
        Assert.AreEqual(true, cbinstructions.Where(op => op is not null).Any());  // Assert has at least one instruction

        List<string> missing_implementation = new List<string>();
        List<Operation> outside_isa = new List<Operation>();
        List<Operation> correctly_implemented = new List<Operation>();
        List<string> correctly_missing = new List<string>();
        List<string> weird = new List<string>();
        for (var i = 0; i < instructions.Length; i++) {
            var hex = $"0x{i:X2}";
            var op = instructions[i];
            if (isa.unprefixed.ContainsKey(hex) && (op is null)) {
                // ISA specifies instruction but CPU doesn't implement it
                missing_implementation.Add(hex);
            } else if (!isa.unprefixed.ContainsKey(hex) && (op is not null)) {
                // CPU defined instruction but it's not in the ISA
                outside_isa.Add(op);
            } else if (isa.unprefixed.ContainsKey(hex) && (op is not null)) {
                // ISA specified the instruction and the CPU implement it
                correctly_implemented.Add(op);
            } else if (!isa.unprefixed.ContainsKey(hex) && (op is null)) {
                // ISA doesn't define the instruction and the CPU doesn't implement is
                correctly_missing.Add(hex);
            } else {
                // Never should I ever get here
                weird.Add(hex);
            }
        }

        for (var i = 0; i < cbinstructions.Length; i++) {
            var hex = $"0x{i:X2}"; var cbHex = $"CB-0x{i:X2}";
            var op = cbinstructions[i];
            if (isa.cbprefixed.ContainsKey(hex) && (op is null)) {
                // ISA specifies instruction but CPU doesn't implement it
                missing_implementation.Add(cbHex);
            } else if (!isa.cbprefixed.ContainsKey(hex) && (op is not null)) {
                // CPU defined instruction but it's not in the ISA
                outside_isa.Add(op);
            } else if (isa.cbprefixed.ContainsKey(hex) && (op is not null)) {
                // ISA specified the instruction and the CPU implement it
                correctly_implemented.Add(op);
            } else if (!isa.cbprefixed.ContainsKey(hex) && (op is null)) {
                // ISA doesn't define the instruction and the CPU doesn't implement is
                correctly_missing.Add(cbHex);
            } else {
                // Never should I ever get here
                weird.Add(cbHex);
            }
        }

        if (missing_implementation.Count > 0) {
            Assert.Fail($"Missing opcode implementations: [{string.Join(',', missing_implementation)}]");
        }
        if (weird.Count > 0) {
            Assert.Fail($"Some opcodes don't any assertions: [{string.Join(',', weird)}]");
        }
    }

    #region Blargg Tests
    [TestMethod]
    public void Test01() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "01-special";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom02() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "02-interrupts";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom03() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "03-op sp,hl";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom04() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "04-op r,imm";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom05() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "05-op rp";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom06() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "06-ld r,r";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom07() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "07-jr,jp,call,ret,rst";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom08() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "08-misc instrs";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom09() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "09-op r,r";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom10() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "10-bit ops";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }

    [TestMethod]
    public void TestRom11() {
        // Get GB
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "11-op a,(hl)";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        RunUntilEnd(gb);

        // Validate results
        ValidateRomOutput(name, results);
    }
    #endregion

    #region Jsmoo Tests
    private void setStateToModel(Cpu cpu, IMemory memory, OpcodeTestStateJsonModel model) {
        // Registry state
        var reg = cpu.Registry;
        reg.pc(model.pc);
        reg.sp(model.sp);
        reg.a(model.a);
        reg.b(model.b);
        reg.c(model.c);
        reg.d(model.d);
        reg.e(model.e);
        reg.f(model.f);
        reg.h(model.h);
        reg.l(model.l);
        reg.ime(model.ime);
        // TODO reg.ie

        // Ram state
        if (model.ram is not null) {
            for (var i = 0; i < model.ram.Length; i++) {
                var address = model.ram[i][0];
                var value = model.ram[i][1];
                memory.WriteByte(address, value);
            }
        }
    }

    private void validateStateWithModel(Operation op, string filename, int testId, Cpu cpu, IMemory memory, OpcodeTestStateJsonModel model) {
        // Registry state
        var reg = cpu.Registry;
        Assert.AreEqual(model.pc, reg.pc()  , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": PC state mismatch");
        Assert.AreEqual(model.sp, reg.sp()  , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": SP state mismatch");
        Assert.AreEqual(model.a , reg.a()   , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": A state mismatch");
        Assert.AreEqual(model.b , reg.b()   , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": B state mismatch");
        Assert.AreEqual(model.c , reg.c()   , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": C state mismatch");
        Assert.AreEqual(model.d , reg.d()   , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": D state mismatch");
        Assert.AreEqual(model.e , reg.e()   , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": E state mismatch");
        Assert.AreEqual(model.f , reg.f()   , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": F state mismatch");
        Assert.AreEqual(model.h , reg.h()   , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": H state mismatch");
        Assert.AreEqual(model.l , reg.l()   , $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": L state mismatch");
        Assert.AreEqual(model.ime, reg.ime(), $"{filename}[{testId}] failed for \"{op.Name}({op.Opcode})\": IME state mismatch");
        // TODO reg.ie

        // Ram state
        if (model.ram is not null) {
            for (var i = 0; i < model.ram.Length; i++) {
                var address = model.ram[i][0];
                var value = model.ram[i][1];
                Assert.AreEqual(value, memory.ReadByte(address), $"{filename}[{testId}] failed for \"{op.Name}\": Memory @{address} incorrect");
            }
        }
    }

    private void doTestFor(Cpu cpu, IMemory memory, string hexName, Operation op, List<Exception> errors, List<Operation> missing) {
        try {
            var json = ReadJson<OpcodeTestJsonModel[]>(hexName);
            for (var testId = 0; testId < json.Length; testId++) {
                var test = json[testId];
                if (test.initial is null)
                    Assert.Fail("Missing initial state for instruction " + hexName);

                if (test.final is null)
                    Assert.Fail("Missing initial state for instruction " + hexName);

                // Initialize the state
                setStateToModel(cpu, memory, test.initial);
                validateStateWithModel(op, hexName, testId, cpu, memory, test.initial);

                // Run the program
                cpu.Step();

                // Validate the end state
                try {
                    validateStateWithModel(op, hexName, testId, cpu, memory, test.final);
                } catch (UnitTestAssertException ex) {
                    errors.Add(ex);
                    break;
                }
            }
        } catch (FileNotFoundException) {
            // Have no tests for this instruction
            missing.Add(op);
        }
    }

    [TestMethod]

    public void TestInstructionsBehaviors() {
        var memory = new FlatRam(DataSize.Kibibytes(64), Endianness.LittleEndian);
        Cpu cpu = new Cpu(memory);
        List<Operation> operations_with_no_tests = new List<Operation>(256);
        List<Exception> errors = new List<Exception>();
        for (var i = 0; i < 256; i++) {
            if (cpu.TryFetchOperation(i, out var op)) {
                if (op is null)
                    continue;
                cpu.Reset();
                memory.Reset();

                // SKIP HALT, CB-PREFIX, and EI (I don't simulate lag), and DJNZn (which may be the wrong op) tests
                if (op.Opcode == 0x76 || op.Opcode == 0xCB || op.Opcode == 251 || op.Opcode == 16)
                    continue;

                var hexName = "opcode_tests." + op.Opcode.ToString("x2");
                doTestFor(cpu, memory, hexName, op, errors, operations_with_no_tests);
            }
        }
        for (var i = 0; i < 256; i++) {
            if (cpu.TryFetchCbPrefixedOperation(i, out var op)) {
                if (op is null)
                    continue;
                cpu.Reset();
                memory.Reset();

                var hexName = "opcode_tests.cb " + op.Opcode.ToString("x2");
                doTestFor(cpu, memory, hexName, op, errors, operations_with_no_tests);
            }
        }
        Console.WriteLine(operations_with_no_tests.Count + " instructions are missing behavioral tests.");
        if (errors.Count > 0) {
            foreach (var error in errors) {
                Console.WriteLine(error.Message);
            }
            Assert.Fail();
        }
    }
    #endregion
}