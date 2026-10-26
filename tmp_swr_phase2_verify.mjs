const base = 'http://localhost:57031/api/women-child-welfare/single-women/register';
const id = '80';

const payload = {
  fullName: 'ASSURE 3797',
  mobileNumber: '9869900546',
  aadhaarNumber: '551818899392',
  rationCardNumber: 'RC-ARCH-80',
  voterIdNumber: 'VN-ARCH-80',
  rationCardType: 'YELLOW',
  rationCardTypeOther: 'LegacyType80',
  email: 'assure80@example.com',
  livelihoodSource: 'Home business',
  isDivyangSingleWoman: 'N',
  residenceType: 'RENTED',
  landStatus: 'NO_LAND',
  landAreaAcre: '0',
  isBpl: 'Y',
  totalChildren: '2',
  boysCount: '1',
  girlsCount: '1',
  childrenBelow18Count: '2',
  childrenInEducation: 'Y',
  getsBalSangopanScholarship: 'N',
  educationQualification: 'HSC',
  currentOccupation: 'Stitching',
  monthlyIncome: '9500',
  womanMonthlyIncome: '9500',
  familyAnnualIncome: '114000',
  isSelfHelpGroupMember: 'Y',
  requiredSupportOther: 'Need market linkage',
  isAvailingGovtScheme: 'Y',
  govtSchemeName: 'Sanjay Gandhi Niradhar',
  wantsSelfEmployment: 'Y',
  expectedBusinessType: 'Tailoring unit',
  skillsJson: '["Tailoring","Machine Operation"]',
  requiredSupportJson: '["Training","Loan"]'
};

const updateRes = await fetch(`${base}/${id}`, {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(payload)
});

const updateText = await updateRes.text();
console.log('PUT_STATUS', updateRes.status);
console.log('PUT_BODY', updateText);

const getRes = await fetch(`${base}/${id}`);
const getJson = await getRes.json();
const row = getJson?.data ?? {};

console.log('GET_STATUS', getRes.status);
console.log('ration_card_number', row.ration_card_number ?? '');
console.log('voter_id_number', row.voter_id_number ?? '');
console.log('email', row.email ?? '');
console.log('total_children', row.total_children ?? '');
console.log('monthly_income', row.monthly_income ?? '');
console.log('skills_json', row.skills_json ?? '');
console.log('required_support_json', row.required_support_json ?? '');
