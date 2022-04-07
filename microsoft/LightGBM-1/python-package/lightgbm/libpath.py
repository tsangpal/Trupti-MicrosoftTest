# coding: utf-8
"""Find the path to lightgbm dynamic library files."""
import os
import sys


def find_lib_path():
    """Find the path to LightGBM library files.
    Returns
    -------
    lib_path: list(string)
       List of all found library path to LightGBM
    """
    curr_path = os.path.dirname(os.path.abspath(os.path.expanduser(__file__)))
    dll_path = [curr_path, os.path.join(curr_path, '../../lib/'),
                os.path.join(curr_path, '../../'),
                os.path.join(curr_path, './lib/'),
                os.path.join(sys.prefix, 'lightgbm')]
    if os.name == 'nt':
        dll_path.append(os.path.join(curr_path, '../../windows/x64/Dll/'))
        dll_path.append(os.path.join(curr_path, './windows/x64/Dll/'))
        dll_path.append(os.path.join(curr_path, '../../Release/'))
        dll_path = [os.path.join(p, 'lib_lightgbm.dll') for p in dll_path]
    else:
        dll_path = [os.path.join(p, 'lib_lightgbm.so') for p in dll_path]
    lib_path = [p for p in dll_path if os.path.exists(p) and os.path.isfile(p)]
    if not lib_path:
        dll_path = [os.path.realpath(p) for p in dll_path]
        raise Exception('Cannot find lightgbm Library in following paths: ' + ','.join(dll_path))
    return lib_path
