#  _________________________________________________________________________
#
#  Pyomo: Python Optimization Modeling Objects
#  Copyright (c) 2014 Sandia Corporation.
#  Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
#  the U.S. Government retains certain rights in this software.
#  This software is distributed under the BSD License.
#  _________________________________________________________________________

import os
from os.path import abspath, dirname
pyomodir = dirname(abspath(__file__))+os.sep+".."+os.sep+".."+os.sep
currdir = dirname(abspath(__file__))+os.sep

import pyutilib.th as unittest
import pyutilib.services
import pyutilib.common

from pyomo.core import ConcreteModel
import pyomo.opt
from pyomo.opt import ResultsFormat, ProblemFormat

try:
    pico_convert =  pyutilib.services.registered_executable("pico_convert")
    pico_convert_available= (not pico_convert is None)
except pyutilib.common.ApplicationError:
    pico_convert_available=False

def filter_cplex(line):
    return line.startswith("Message:")

old_ignore_time = None
old_tempdir = None
def setUpModule():
    global old_tempdir
    global old_ignore_time
    old_tempdir = pyutilib.services.TempfileManager.tempdir
    old_ignore_time = pyomo.opt.SolverResults.default_print_options.ignore_time
    pyomo.opt.SolverResults.default_print_options.ignore_time = True
    pyutilib.services.TempfileManager.tempdir = currdir

def tearDownModule():
    pyutilib.services.TempfileManager.tempdir = old_tempdir
    pyomo.opt.SolverResults.default_print_options.ignore_time = old_ignore_time

cplexamp_available = False
class mock_all(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        global cplexamp_available
        import pyomo.environ
        from pyomo.solvers.tests.io.writer_test_cases import SolverTestCase
        cplexamp_available = SolverTestCase(name='cplex',io='nl').available
        
    def setUp(self):
        self.do_setup(False)

    def do_setup(self,flag):
        global tmpdir
        tmpdir = os.getcwd()
        os.chdir(currdir)
        pyutilib.services.TempfileManager.sequential_files(0)
        if flag:
            if not cplexamp_available:
                self.skipTest("The 'cplexamp' command is not available")
            self.asl = pyomo.opt.SolverFactory('asl:cplexamp')
        else:
            self.asl = pyomo.opt.SolverFactory('_mock_asl:cplexamp')

    def tearDown(self):
        global tmpdir
        pyutilib.services.TempfileManager.clear_tempfiles()
        pyutilib.services.TempfileManager.unique_files()
        os.chdir(tmpdir)
        if self.asl is not None:
            self.asl.deactivate()

    def test_path(self):
        """ Verify that the ASL path is what is expected """
        if type(self.asl) == 'ASL':
            self.assertEqual(self.asl.executable.split(os.sep)[-1],
                             "ASL"+pyomo.util.executable_extension)

    def Xtest_solve1(self):
        """ Test ASL - test1.mps """
        results = self.asl.solve(currdir+"test1.mps",
                                 logfile=currdir+"test_solve1.log",
                                 suffixes=['.*'])
        results.write(filename=currdir+"test_solve1.txt",
                      times=False,
                      format='json')
        self.assertMatchesJsonBaseline(currdir+"test_solve1.txt",
                                       currdir+"test1_asl.txt")
        #os.remove(currdir+"test_solve1.log")

    def Xtest_solve2a(self):
        """ Test ASL - test1.mps """
        results = self.asl.solve(currdir+"test1.mps",
                                 rformat=ResultsFormat.soln,
                                 logfile=currdir+"test_solve2a.log",
                                 suffixes=['.*'])
        results.write(filename=currdir+"test_solve2a.txt",
                      times=False,
                      format='json')
        self.assertMatchesJsonBaseline(currdir+"test_solve2a.txt",
                                       currdir+"test1_asl.txt")
        #os.remove(currdir+"test_solve2a.log")

    def Xtest_solve2b(self):
        """ Test ASL - test1.mps """
        results = self.asl.solve(currdir+"test1.mps",
                                 pformat=ProblemFormat.mps,
                                 rformat=ResultsFormat.soln,
                                 logfile=currdir+"test_solve2b.log",
                                 suffixes=['.*'])
        results.write(filename=currdir+"test_solve2b.txt",
                      times=False,
                      format='json')
        self.assertMatchesJsonBaseline(currdir+"test_solve2b.txt",
                                       currdir+"test1_asl.txt")
        #os.remove(currdir+"test_solve2b.log")

    def Xtest_solve3(self):
        """ Test ASL - test2.lp """
        results = self.asl.solve(currdir+"test2.lp",
                                 logfile=currdir+"test_solve3.log",
                                 keepfiles=True,
                                 suffixes=['.*'])
        results.write(filename=currdir+"test_solve3.txt",
                      times=False,
                      format='json')
        self.assertMatchesJsonBaseline(currdir+"test_solve3.txt",
                                       currdir+"test2_asl.txt")
        if os.path.exists(currdir+"test2.solution.dat"):
            os.remove(currdir+"test2.solution.dat")
        #os.remove(currdir+"test_solve3.log")

    def test_solve4(self):
        """ Test ASL - test4.nl """
        results = self.asl.solve(currdir+"test4.nl",
                                 logfile=currdir+"test_solve4.log",
                                 suffixes=['.*'])
        results.write(filename=currdir+"test_solve4.txt",
                      times=False,
                      format='json')
        self.assertMatchesJsonBaseline(currdir+"test_solve4.txt",
                                       currdir+"test4_asl.txt",
                                       tolerance=1e-4)
        os.remove(currdir+"test_solve4.log")
        if os.path.exists(currdir+"test4.soln"):
            os.remove(currdir+"test4.soln")

    #
    # This test is disabled, but it's useful for interactively exercising
    # the option specifications of a solver
    #
    def Xtest_options(self):
        """ Test ASL options behavior """
        results = self.asl.solve(currdir+"bell3a.mps",
                                 logfile=currdir+"test_options.log",
                                 options="sec=0.1 foo=1 bar='a=b c=d' xx_zz=yy",
                                 suffixes=['.*'])
        results.write(filename=currdir+"test_options.txt",
                      times=False)
        self.assertFileEqualsBaseline(currdir+"test_options.txt",
                                      currdir+  "test4_asl.txt")
        #os.remove(currdir+"test4.sol")
        #os.remove(currdir+"test_solve4.log")

    def Xtest_mock5(self):
        """ Mock Test ASL - test5.mps """
        results = self.asl.solve(currdir+"test4.nl",
                                 logfile=currdir+"test_solve5.log",
                                 keepfiles=True,
                                 suffixes=['.*'])
        results.write(filename=currdir+"test_mock5.txt",
                      times=False)
        self.assertFileEqualsBaseline(currdir+"test_mock5.txt",
                                      currdir+"test4_asl.txt")
        os.remove(currdir+"test4.sol")
        os.remove(currdir+"test_solve5.log")

    def test_error1(self):
        """ Bad results format """
        try:
            model = ConcreteModel()
            results = self.asl.solve(model,
                                     format=ResultsFormat.sol,
                                     suffixes=['.*'])
            self.fail("test_error1")
        except ValueError:
            pass

    def test_error2(self):
        """ Bad solve option """
        try:
            model = ConcreteModel()
            results = self.asl.solve(model,
                                     foo="bar")
            self.fail("test_error2")
        except ValueError:
            pass

    def test_error3(self):
        """ Bad solve option """
        try:
            results = self.asl.solve(currdir+"model.py",
                                     foo="bar")
            self.fail("test_error3")
        except ValueError:
            pass

class mip_all(mock_all):

    def setUp(self):
        self.do_setup(True)


if __name__ == "__main__":
    unittest.main()
