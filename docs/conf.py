# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# -- Path setup --------------------------------------------------------------

# If extensions (or modules to document with autodoc) are in another directory,
# add these directories to sys.path here. If the directory is relative to the
# documentation root, use os.path.abspath to make it absolute, like shown here.
#

import os
import platform
#import sys
# sys.path.insert(0, os.path.abspath('.'))


# -- Project information -----------------------------------------------------

project = 'DEWETRON TRION SDK'
copyright = '2021, DEWETRON GmbH'
author = 'DEWETRON GmbH'
nav_title = 'DEWETRON TRION SDK'

# The full version, including alpha/beta/rc tags
release = '7.0'


# -- General configuration ---------------------------------------------------

# Add any Sphinx extension module names here, as strings. They can be
# extensions coming with Sphinx (named 'sphinx.ext.*') or your custom
# ones.
extensions = [
   "sphinx.ext.imgmath"
]

# Add any paths that contain templates here, relative to this directory.
templates_path = ['_templates']

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = []


# Formula latex support
if platform.system() == 'Windows':
   imgmath_latex="latex.exe"

# color in formulas
imgmath_latex_preamble=r'\usepackage{xcolor}'

# -- Options for HTML output -------------------------------------------------

# The theme to use for HTML and HTML Help pages.  See the documentation for
# a list of builtin themes.
#
#html_short_title = 'ODK'
#html_theme = 'sphinx_rtd_theme'
html_theme = 'sphinx_material'
html_logo = '_img/logo.png'
#html_favicon = '_img/oxygen.ico'
html_show_sourcelink = False
html_copy_source = False

# Add any paths that contain custom static files (such as style sheets) here,
# relative to this directory. They are copied after the builtin static files,
# so a file named "default.css" will overwrite the builtin "default.css".
html_static_path = ['_static']


if html_theme == 'sphinx_material':
   html_css_files = [
      'stylesheets/oxygen_material.css'
   ]
   html_theme_options = {
      # Set the name of the project to appear in the navigation.
      'nav_title': nav_title,

      # Set the color and the accent color
      'color_primary': 'blue',
      'color_accent': 'light-blue',

      # Set the repo location to get a badge with stats
      #'repo_url': 'https://github.com/bashtage/sphinx-material',
      #'repo_name': 'sphinx-material',

      # Visible levels of the global TOC; -1 means unlimited
      'globaltoc_depth': 0,
      # If False, expand all TOC entries
      'globaltoc_collapse': True,
      # If True, show hidden TOC entries
      'globaltoc_includehidden': True,

      # internal & external links
      "nav_links": [

        {
            "href": "https://www.dewetron.com/products/signal-conditioning/all-trion3-trion-modules/",
            "internal": False,
            "title": "TRION & TRION3 Modules",
        },
        {
            "href": "https://www.dewetron.com/products/oxygen-measurement-software/",
            "internal": False,
            "title": "OXYGEN Software",
        },
        {   
            "href": "https://www.dewetron.com/",
            "internal": False,
            "title": "DEWETRON Measurement Systems",
        },
    ],

   }
   html_sidebars = {
         "**": ["logo-text.html", "globaltoc.html", "localtoc.html", "searchbox.html"]
      }



# -- Options for Latex/PDF output -------------------------------------------------
# see https://www.sphinx-doc.org/en/master/latex.html

latex_engine = 'lualatex'  #to use ttf fonts

latex_maketitle = r'''
\includepdf{Cover_en.pdf}
% \de{\includepdf{Cover_de.pdf}}
'''

latex_elements = {
# The paper size ('letterpaper' or 'a4paper').
'papersize': 'a4paper',

# The font size ('10pt', '11pt' or '12pt').
'pointsize': '10pt',

# fonts, better vertical aligned tables
'preamble' : r'\input{dewetron_style.tex.txt}',

'maketitle': latex_maketitle,

# fncychap
'fncychap' : r'\usepackage[Bjarne]{fncychap}',
#'fncychap' : r'\usepackage[Sonny]{fncychap}',

#Figure placement within LaTeX paper
'figure_align': 'H',
}

latex_additional_files = [
   "_static/dewetron_style.tex.txt",
   "_static/Cover_en.pdf"
]

numfig = True