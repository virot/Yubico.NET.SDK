// Copyright 2024 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using Yubico.PlatformInterop;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivBioMultiProtocolTests
    {

        /// <summary>
        /// Verify authentication with YubiKey Bio Multi-protocol
        /// </summary>
        /// <remarks>
        /// To run the test, create a PIN and enroll at least one fingerprint. The test will ask twice
        /// for fingerprint authentication.
        /// <para>
        /// Tests with devices without Bio Metadata are skipped.
        /// </remarks>
        /// <param name="testDeviceType"></param>
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void PivBioMultiProtocol_Authenticate(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            using (var pivSession = new PivSession(testDevice))
            {
                var bioMetadata = pivSession.GetBioMetadata();
                var connection = pivSession.Connection;

                Assert.True(VerifyUv(connection, false, false).IsEmpty);
                Assert.False(pivSession.GetBioMetadata().HasTemporaryPin);                

                // check verified state
                Assert.True(VerifyUv(connection, false, true).IsEmpty);

                var temporaryPin = VerifyUv(connection, true, false);
                Assert.False(temporaryPin.IsEmpty);
                Assert.True(pivSession.GetBioMetadata().HasTemporaryPin);

                // check verified state
                Assert.True(VerifyUv(connection, false, true).IsEmpty);

                VerifyTemporaryPin(connection, temporaryPin);
            }
        }

        private ReadOnlyMemory<byte> VerifyUv(IYubiKeyConnection connection, bool requestTemporaryPin, bool checkOnly)
        {
            var command = new VerifyUvCommand(requestTemporaryPin, checkOnly);
            var response = connection.SendCommand(command);
            return response.GetData();
        }

        private void VerifyTemporaryPin(IYubiKeyConnection connection, ReadOnlyMemory<byte> temporaryPin)
        {
            var command = new VerifyTemporaryPinCommand(temporaryPin);
            _ = connection.SendCommand(command).GetData();
        }
    }
}
