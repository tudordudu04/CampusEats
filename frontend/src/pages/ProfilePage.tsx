import { useEffect, useState } from "react";
import { AuthApi,UserDto } from "../services/api";
import { useLanguage } from '../contexts/LanguageContext';

export default function ProfilePage() {
    const [user, setUser] = useState<UserDto | null>(null);
    const [loading, setLoading] = useState(true);
    const { language } = useLanguage();
    const[formData,setFormData]=useState({
        name:'',
        addressCity:'',
        addressStreet:'',
        addressNumber:'',
        addressDetails:'',
        profilePictureUrl:''
    });
    useEffect(() => {
        const loadProfile = async () => {
            try{
                const data = await AuthApi.getMe();
                setUser(data);
                setFormData({
                    name:data.name || '',
                    addressCity:data.addressCity || '',
                    addressStreet:data.addressStreet || '',
                    addressNumber:data.addressNumber || '',
                    addressDetails:data.addressDetails || '',
                    profilePictureUrl:data.profilePictureUrl || ''
                });
            }
            catch(err){
                alert((language === 'ro' ? 'Eroare la încărcarea profilului' : 'Profile load error') + ": " + (err as Error).message);
            }
            finally{
                setLoading(false);
            }
        }
        loadProfile();
    }, []);

    if(loading){
        return <div className="p-10 text-center dark:text-slate-300">{language === 'ro' ? 'Se încarcă...' : 'Loading...'}</div>
    }
    const handleSubmit=async(e : React.FormEvent)=>{
        e.preventDefault();
        console.log("Datele trimise pentru actualizare:", formData);
        try{
            await AuthApi.updateProfile(formData);
            alert(language === 'ro' ? 'Profil actualizat cu succes!' : 'Profile updated successfully!');
        }
        catch(err){
            console.error(err);
            alert((language === 'ro' ? 'Eroare la actualizarea profilului' : 'Profile update error') + ": " + (err as Error).message);
        }
    }

   return (
        <div className="p-4 max-w-2xl mx-auto">
            <h1 className="text-2xl font-bold mb-6 dark:text-slate-100">{language === 'ro' ? 'Profilul Meu' : 'My Profile'}</h1>

     
            <form onSubmit={handleSubmit} className="space-y-4">
                
               
                
                <div className="mb-4">
                    <label className="block font-bold mb-1 dark:text-slate-200">{language === 'ro' ? 'Email' : 'Email'}:</label>
                    <input 
                        type="email" 
                        value={user?.email || ''} 
                        disabled={true}
                        className="border dark:border-slate-600 p-2 rounded w-full bg-gray-100 dark:bg-slate-700 text-gray-500 dark:text-slate-400 cursor-not-allowed"
                    />
                </div>

                <div className="mb-4">
                    <label className="block font-bold mb-1 dark:text-slate-200">{language === 'ro' ? 'Nume' : 'Name'}:</label>
                    <input 
                        type="text" 
                        className="border dark:border-slate-600 p-2 rounded w-full dark:bg-slate-700 dark:text-slate-100"
                        value={formData.name} 
                        onChange={e => setFormData({...formData, name: e.target.value})} 
                    />
                </div>

           
                <div className="mb-4">
                    <label className="block font-bold mb-1 dark:text-slate-200">{language === 'ro' ? 'Poză Profil' : 'Profile Picture'}:</label>
                    {formData.profilePictureUrl && (
                        <img 
                            src={formData.profilePictureUrl}
                            alt="Poză Profil"
                            className="w-20 h-20 rounded-full object-cover mb-2 border border-gray-300 dark:border-slate-600"
                        />
                    )}
                    <input 
                        type="file"
                        accept = "image/*"
                        className="block w-full text-sm text-gray-900 dark:text-slate-100 border border-gray-300 dark:border-slate-600 rounded-lg cursor-pointer bg-gray-50 dark:bg-slate-700 focus:outline-none file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-brand-600 file:text-white hover:file:bg-brand-700 file:cursor-pointer"
                        onChange = {async e => {
                            const file = e.target.files?.[0];
                            if(!file){
                                return
                            } 
                            try{
                                const result = await AuthApi.uploadProfilePicture(file);
                                console.log("1. raspuns primit după upload:", result);
                                setFormData({...formData, profilePictureUrl: result.url});
                                alert(language === 'ro' ? 'Poză încărcată cu succes!' : 'Picture uploaded successfully!');
                            }
                            catch(err){
                                console.error(err);
                                alert(language === 'ro' ? 'Eroare la încărcarea pozei' : 'Error uploading picture');
                            }

                        }}
                         />
                </div>
                <div className="mb-4">
                    <label className="block font-bold mb-1 dark:text-slate-200">{language === 'ro' ? 'Oraș' : 'City'}:</label>
                    <input type="text" className="border dark:border-slate-600 p-2 rounded w-full dark:bg-slate-700 dark:text-slate-100" value={formData.addressCity} onChange={e => setFormData({...formData, addressCity: e.target.value})} />
                </div>
                <div className="mb-4">
                    <label className="block font-bold mb-1 dark:text-slate-200">{language === 'ro' ? 'Stradă' : 'Street'}:</label>
                    <input type="text" className="border dark:border-slate-600 p-2 rounded w-full dark:bg-slate-700 dark:text-slate-100" value={formData.addressStreet} onChange={e => setFormData({...formData, addressStreet: e.target.value})} />
                </div>
                <div className="grid grid-cols-2 gap-4">
                    <div className="mb-4">
                        <label className="block font-bold mb-1 dark:text-slate-200">{language === 'ro' ? 'Număr' : 'Number'}:</label>
                        <input type="text" className="border dark:border-slate-600 p-2 rounded w-full dark:bg-slate-700 dark:text-slate-100" value={formData.addressNumber} onChange={e => setFormData({...formData, addressNumber: e.target.value})} />
                    </div>
                    <div className="mb-4">
                        <label className="block font-bold mb-1 dark:text-slate-200">{language === 'ro' ? 'Detalii' : 'Details'}:</label>
                        <input type="text" className="border dark:border-slate-600 p-2 rounded w-full dark:bg-slate-700 dark:text-slate-100" value={formData.addressDetails} onChange={e => setFormData({...formData, addressDetails: e.target.value})} />
                    </div>
                </div>

             
                <button 
                    type="submit" 
                    className="w-full bg-brand-600 text-white font-bold py-3 px-4 rounded hover:bg-brand-700 transition duration-200 mt-4"
                >
                    {language === 'ro' ? 'Salvează Modificările' : 'Save Changes'}
                </button>

            </form>
        </div>
    )
}