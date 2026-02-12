import{j as a,an as e,_ as t}from"./index-DXStjdCc.js";function i(){return a.jsxs("div",{className:"relative min-h-screen overflow-hidden bg-gradient-to-br from-green-50 via-white via-green-100 to-blue-50 flex items-center justify-center",children:[a.jsx("div",{className:"absolute inset-0 opacity-30 pointer-events-none",style:{backgroundImage:"radial-gradient(circle, #000 1px, transparent 1px)",backgroundSize:"30px 30px"}}),a.jsx("div",{className:"absolute inset-0 opacity-10 pointer-events-none",style:{background:"repeating-linear-gradient(45deg, transparent, transparent 10px, rgba(255, 255, 255, 0.5) 10px, rgba(255, 255, 255, 0.5) 11px)"}}),a.jsxs("div",{className:"absolute inset-0 pointer-events-none overflow-hidden",children:[a.jsx("div",{className:"absolute w-20 h-20 bg-[#197853] rounded-full opacity-15 top-[10%] left-[10%] animate-float"}),a.jsx("div",{className:"absolute w-16 h-16 bg-gray-800 rounded-xl opacity-15 top-[70%] right-[15%] animate-float-delayed"}),a.jsx("div",{className:"absolute w-24 h-24 bg-gray-600 rounded-full opacity-15 bottom-[15%] left-[20%] animate-float-more-delayed"})]}),a.jsxs("div",{className:"relative z-10 text-center px-10 py-12 max-w-2xl animate-fade-in-up",children:[a.jsx("div",{className:"text-[150px] font-black text-gray-800 leading-none mb-5 animate-glitch",children:"404"}),a.jsx("h1",{className:"text-4xl font-bold text-gray-900 mb-4",children:"Səhifə Tapılmadı"}),a.jsx("p",{className:"text-lg text-gray-600 mb-10 leading-relaxed",children:"Üzr istəyirik, axtardığınız səhifə mövcud deyil və ya köçürülüb. Zəhmət olmasa ana səhifəyə qayıdın və ya məhsulları nəzərdən keçirin."}),a.jsxs("div",{className:"flex gap-4 justify-center flex-wrap",children:[a.jsxs("button",{onClick:()=>window.location.href="/",className:"flex items-center gap-3 px-10 py-4 bg-[#197853] text-white font-semibold rounded-lg shadow-lg hover:bg-[#0A4A30] hover:-translate-y-0.5 hover:shadow-xl transition-all duration-300",children:[a.jsx(e,{className:"w-5 h-5"}),"Ana Səhifə"]}),a.jsxs("button",{onClick:()=>window.location.href="/products",className:"flex items-center gap-3 px-10 py-4 bg-white text-gray-800 font-semibold rounded-lg shadow-lg border-2 border-gray-200 hover:bg-gray-50 hover:-translate-y-0.5 hover:shadow-xl transition-all duration-300",children:[a.jsx(t,{className:"w-5 h-5"}),"Məhsullar"]})]})]}),a.jsx("style",{jsx:!0,children:`
        @keyframes fade-in-up {
          from {
            opacity: 0;
            transform: translateY(30px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }

        @keyframes glitch {
          0%, 100% {
            transform: translate(0);
          }
          20% {
            transform: translate(-2px, 2px);
          }
          40% {
            transform: translate(-2px, -2px);
          }
          60% {
            transform: translate(2px, 2px);
          }
          80% {
            transform: translate(2px, -2px);
          }
        }

        @keyframes float {
          0%, 100% {
            transform: translateY(0) rotate(0deg);
          }
          50% {
            transform: translateY(-30px) rotate(180deg);
          }
        }

        .animate-fade-in-up {
          animation: fade-in-up 0.8s ease;
        }

        .animate-glitch {
          animation: glitch 3s infinite;
        }

        .animate-float {
          animation: float 20s infinite ease-in-out;
        }

        .animate-float-delayed {
          animation: float 20s infinite ease-in-out;
          animation-delay: 5s;
        }

        .animate-float-more-delayed {
          animation: float 20s infinite ease-in-out;
          animation-delay: 10s;
        }

        @media (max-width: 768px) {
          .text-[150px] {
            font-size: 100px;
          }
        }
      `})]})}export{i as default};
