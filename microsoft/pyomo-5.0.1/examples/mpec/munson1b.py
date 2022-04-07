#  _________________________________________________________________________
#
#  Pyomo: Python Optimization Modeling Objects
#  Copyright (c) 2014 Sandia Corporation.
#  Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
#  the U.S. Government retains certain rights in this software.
#  This software is distributed under the BSD License.
#  _________________________________________________________________________
#
# munson1b.py
#
# Problem adapted from munson1.mod
# http://www.ampl.com/NEW/COMPLEMENT/munson1.mod
#
# Negated variables
#

from pyomo.environ import *
from pyomo.mpec import *


model = ConcreteModel()

model.x1 = Var()
model.x2 = Var()
model.x3 = Var()

model.f1 = Complementarity(expr=\
                complements(model.x1 <= 0, \
                            - model.x1 - 2*model.x2 - 3*model.x3 >= 1))

model.f2 = Complementarity(expr=\
                complements(model.x2 <= 0, \
                            - model.x2 + model.x3 >= -1))

model.f3 = Complementarity(expr=\
                complements(model.x3 <= 0, \
                            - model.x1 - model.x2 >= -1))

