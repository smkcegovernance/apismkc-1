const reg = 'WCWC-20260803-000995';
const base = 'http://localhost:3001/api/women-child-welfare/register';

const ynAhe = (v) => String(v || '').toUpperCase() === 'Y' ? 'आहे' : 'नाही';
const ynHoy = (v) => String(v || '').toUpperCase() === 'Y' ? 'होय' : 'नाही';
const fmtDate = (v) => {
  if (!v) return '';
  const s = String(v);
  return s.includes('T') ? s.split('T')[0] : s;
};

const getRes = await fetch(`${base}/${encodeURIComponent(reg)}`);
const getJson = await getRes.json();
if (!getJson.success) {
  console.log('GET_FAILED', getJson);
  process.exit(1);
}

const row = (getJson.data?.registration || [])[0] || {};
const disabilities = (getJson.data?.disabilities || []).map((d) => d.DISABILITY_NAME_EN || '').filter(Boolean);
if (!disabilities.length) disabilities.push('Locomotor Disability');
const devices = (getJson.data?.devices || []).map((d) => d.DEVICE_NAME || '').filter(Boolean);

const payload = {
  applicationMode: row.APPLICATION_MODE || 'DEPARTMENT',
  submissionChannel: row.SUBMISSION_CHANNEL || 'DEPARTMENT_OPERATOR',
  surname: row.SURNAME || '',
  firstName: row.FIRST_NAME || '',
  fatherName: row.FATHER_NAME || '',
  motherName: row.MOTHER_NAME || '',
  education: row.EDUCATION || '',
  aadhaarNumber: row.AADHAAR_NUMBER || '',
  rationCardNumber: 'TEST-RC-995',
  rationCardColor: 'ORANGE',
  dob: fmtDate(row.DOB),
  maritalStatus: row.MARITAL_STATUS || '',
  religion: row.RELIGION || '',
  caste: row.CASTE_NAME || '',
  familyRelation: row.FAMILY_RELATION || '',
  fullAddress: row.FULL_ADDRESS || '',
  wardNumber: String(row.WARD_NUMBER || ''),
  prabhagSamiti: String(row.PRABHAG_SAMITI || ''),
  uphc: row.UPHC || '',
  pincode: String(row.PINCODE || ''),
  constituency: row.CONSTITUENCY || '',
  mobileNumber: String(row.MOBILE_NUMBER || ''),
  alternatePhone: String(row.ALTERNATE_PHONE || ''),
  bankName: row.BANK_NAME || '',
  branchName: row.BRANCH_NAME || '',
  accountNumber: String(row.ACCOUNT_NUMBER || ''),
  ifscCode: row.IFSC_CODE || '',
  bplNumber: row.BPL_NUMBER || '',
  bplYear: String(row.BPL_YEAR || ''),
  hasCertificate: ynAhe(row.HAS_CERTIFICATE),
  certificateNumber: row.CERTIFICATE_NUMBER || '',
  certificateDate: fmtDate(row.CERTIFICATE_DATE),
  certificateType: row.CERTIFICATE_TYPE || '',
  disabilityPercentage: String(row.DISABILITY_PERCENTAGE || ''),
  hasUDID: ynAhe(row.HAS_UDID),
  udidNumber: row.UDID_NUMBER || '',
  hasSTPass: ynAhe(row.HAS_ST_PASS),
  stPassNumber: row.ST_PASS_NUMBER || '',
  hasRailwayPass: ynAhe(row.HAS_RAILWAY_PASS),
  railwayPassNumber: row.RAILWAY_PASS_NUMBER || '',
  hasMSRTCPass: ynAhe(row.HAS_MSRTC_PASS),
  msrtcPassNumber: row.MSRTC_PASS_NUMBER || '',
  isEmployed: ynAhe(row.IS_EMPLOYED),
  employmentType: row.EMPLOYMENT_TYPE || '',
  occupation: row.OCCUPATION || '',
  hasGovtBenefit: ynHoy(row.HAS_GOVT_BENEFIT),
  govtBenefitScheme: row.GOVT_BENEFIT_SCHEME || '',
  hasMCBenefit: ynHoy(row.HAS_MC_BENEFIT),
  mcBenefitDetails: row.MC_BENEFIT_DETAILS || '',
  hasSGNPension: ynHoy(row.HAS_SGN_PENSION),
  hasOwnHouse: ynHoy(row.HAS_OWN_HOUSE),
  wantsHousingBenefit: ynHoy(row.WANTS_HOUSING_BENEFIT),
  hasOwnLand: ynHoy(row.HAS_OWN_LAND),
  hasGuardianship: ynHoy(row.HAS_GUARDIANSHIP),
  guardianName: row.GUARDIAN_NAME || '',
  guardianAddress: row.GUARDIAN_ADDRESS || '',
  guardianPhone: String(row.GUARDIAN_PHONE || ''),
  needsAssistiveDevice: ynHoy(row.NEEDS_ASSISTIVE_DEVICE),
  documentsSubmitted: ynHoy(row.DOCUMENTS_SUBMITTED),
  applicantSignDate: fmtDate(row.APPLICANT_SIGN_DATE),
  surveyorName: row.SURVEYOR_NAME || '',
  surveyorDesignation: row.SURVEYOR_DESIGNATION || '',
  surveyorMobile: String(row.SURVEYOR_MOBILE || ''),
  termsAccepted: true,
  disabilitySelections: disabilities,
  assistiveDeviceSelections: devices,
};

const fd = new FormData();
fd.append('payload', new Blob([JSON.stringify(payload)], { type: 'application/json' }), 'payload.json');

const putRes = await fetch(`${base}/${encodeURIComponent(reg)}`, { method: 'PUT', body: fd });
const putText = await putRes.text();
console.log('PUT_STATUS', putRes.status);
console.log('PUT_BODY', putText);

const verifyRes = await fetch(`${base}/${encodeURIComponent(reg)}`);
const verify = await verifyRes.json();
const row2 = (verify.data?.registration || [])[0] || {};
console.log('AFTER_RATION_NO', row2.RATION_CARD_NUMBER || '');
console.log('AFTER_RATION_COLOR', row2.RATION_CARD_COLOR || '');
