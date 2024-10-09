"""
Copyright DEWETRON GmbH 2024

TRION SDK - Setup Tools Definition
"""
import setuptools

setuptools.setup(
    name="trion_sdk",
    version="7.1.0",
    author="Matthias Straka",
    author_email="matthias.straka@dewetron.com",
    description="Python module to access the Dewetron TRION API",
    license="MIT",
    long_description=open("README.md", "r", encoding="utf-8").read(),
    long_description_content_type="text/markdown",
    url="https://github.com/DEWETRON/TRION-SDK",
    keywords="Measurement, Signal processing, Storage",
    project_urls={
        "Tracker": "https://github.com/DEWETRON/TRION-SDK/issues",
        "Source": "https://github.com/DEWETRON/TRION-SDK",
    },
    classifiers=[
        "Development Status :: 5 - Production/Stable",
        "Intended Audience :: Science/Research",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Programming Language :: Python",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
        "Topic :: Software Development :: Libraries",
        "Topic :: Scientific/Engineering",
        "Operating System :: Microsoft :: Windows",
        "Operating System :: POSIX :: Linux",
    ],
    platforms=["Windows", "Linux"],
    packages=["trion_sdk"],
    package_dir={"trion_sdk": "trion_api/python"},
    python_requires=">=3.9",
)
